using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CAHSentinel.Core;
using WinForms = System.Windows.Forms;

namespace CAHSentinel;

public partial class PetWindow : Window
{
    private readonly LocalStateReader _reader = new();
    private readonly SpriteCatalog _sprites = new();
    private readonly AnimationSelection _selection = new();
    private readonly GifPlayer _player;
    private CancellationTokenSource? _pollCancellation;
    private int _generation;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly WinForms.NotifyIcon _tray;
    private readonly WinForms.ToolStripMenuItem _pauseItem;
    private SentinelSettings _settings = SettingsStore.Load();
    private readonly HashSet<string> _seen = SettingsStore.LoadSeen();
    private TaskSnapshot? _current;
    private bool _polling;
    private bool _exiting;
    private bool _offlineAlerted;

    public PetWindow()
    {
        InitializeComponent();
        _player = new GifPlayer(image => {
            PetImage.Source = image;
            Placeholder.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
        });
        _tray = new WinForms.NotifyIcon {
            Visible = true, Icon = System.Drawing.SystemIcons.Application, Text = "CAH Sentinel"
        };
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("显示桌宠", null, (_, _) => Dispatcher.Invoke(ShowPet));
        _pauseItem = new WinForms.ToolStripMenuItem("暂停监控");
        _pauseItem.Click += (_, _) => Dispatcher.Invoke(ToggleMonitoring);
        menu.Items.Add(_pauseItem);
        menu.Items.Add("设置", null, (_, _) => Dispatcher.Invoke(OpenSettings));
        menu.Items.Add("打开 Foreground", null, (_, _) => Dispatcher.Invoke(OpenForeground));
        menu.Items.Add("测试 Windows 通知", null, (_, _) =>
            Dispatcher.Invoke(() => ShowNotification("CAH Sentinel 测试", "Windows 桌面通知通道正常。", false)));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出 Sentinel（停止监控）", null, (_, _) => Dispatcher.Invoke(Quit));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowPet);
        _tray.BalloonTipClicked += (_, _) => Dispatcher.Invoke(OpenForeground);

        _timer.Tick += async (_, _) => await PollAsync();
        StateLabel.Text = "INIT";
        TaskLabel.Text = "等待选择 CAH 本地目录";
        Render(_settings.Monitoring ? PetMood.Idle : PetMood.Paused, "等待配置");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Right - Width - 22;
        Top = SystemParameters.WorkArea.Bottom - Height - 12;
        DesktopPosition.Restore(this, _settings);
        Visibility = _settings.ShowPet ? Visibility.Visible : Visibility.Hidden;
        UpdateMenu();
        if (_settings.Monitoring) _timer.Start();
        if (string.IsNullOrWhiteSpace(_settings.LocalRepoPath))
            _ = Dispatcher.BeginInvoke(new Action(OpenSettings));
        else
            await PollAsync();
    }

    private async Task PollAsync()
    {
        if (_exiting || _polling || !_settings.Monitoring || string.IsNullOrWhiteSpace(_settings.LocalRepoPath))
            return;
        _polling = true;
        var generation = _generation;
        using var cancellation = new CancellationTokenSource();
        _pollCancellation = cancellation;
        try
        {
            // Read only local Git objects already refreshed by CAH; no network fetch or authentication.
            var snapshot = await _reader.ReadAsync(_settings.LocalRepoPath, cancellation.Token);
            if (_exiting || !_settings.Monitoring || generation != _generation) return;
            _current = snapshot;
            _offlineAlerted = false;
            var focus = _current.PriorityTask;
            var animation = _selection.Observe(_current, _seen);
            SettingsStore.SaveSeen(_seen);
            var displayTask = animation.PlayOnce
                ? _current.Tasks.First(t => t.EventKey == animation.EventKey) : focus;
            Render(animation.Mood, displayTask?.Title ?? "当前没有 Task Cell", animation);
            if (_settings.Notifications && animation.PlayOnce)
            {
                var completed = _current.Tasks.First(t => t.EventKey == animation.EventKey);
                ShowNotification("CAH 父任务已验收", completed.Title + " · 查看交付", false);
            }
            foreach (var task in _current.Tasks)
            {
                if (!_settings.Notifications) continue;
                var urgent = task.Mood is PetMood.WaitUser or PetMood.Blocked or PetMood.Error;
                if (!urgent || _seen.Contains(task.EventKey)) continue;
                var (caption, text) = task.Mood switch
                {
                    PetMood.WaitUser => ("CAH 需要你的回复", task.Title + " · " + task.Question),
                    PetMood.Blocked => ("CAH 任务受阻", task.Title + " · 请检查 Planner / Worker 记录"),
                    PetMood.Error => ("CAH 出现错误", task.Title + " · 查看任务状态"),
                    _ => ("CAH 任务完成", task.Title + " · 查看交付")
                };
                ShowNotification(caption, text, task.Mood != PetMood.Complete);
                _seen.Add(task.EventKey);
                SettingsStore.SaveSeen(_seen);
            }
        }
        catch (Exception ex)
        {
            if (_exiting || !_settings.Monitoring || generation != _generation || cancellation.IsCancellationRequested) return;
            _selection.Reset(); // Reconnection establishes a new historical completion baseline.
            Render(PetMood.Disconnected, ex is StaleLocalStateException ? "本地状态过期" : "本地状态读取失败");
            if (_settings.Notifications && !_offlineAlerted)
            {
                ShowNotification("CAH Sentinel 本地状态不可用",
                    ex is StaleLocalStateException ? "CAH 的本地状态超过 5 分钟没有刷新，请检查 CAH Bridge。" :
                    "本地 Git 状态读取失败：" + ex.GetType().Name + "。请检查目录与本地 Git。", true);
                _offlineAlerted = true;
            }
        }
        finally
        {
            _pollCancellation = null;
            _polling = false;
            if (!_exiting && _settings.Monitoring && generation != _generation) _ = PollAsync();
        }
    }

    private void Render(PetMood mood, string task, AnimationIntent? animation = null)
    {
        var name = mood switch
        {
            PetMood.WaitUser => "需要人工确认",
            PetMood.WaitHelper => "Helper 正在处理",
            PetMood.Blocked => "任务受阻",
            PetMood.Error => "执行错误",
            PetMood.Complete => "任务完成",
            PetMood.Running => "运行中",
            PetMood.Disconnected => "监控失联",
            PetMood.Paused => "已暂停",
            _ => "待机中"
        };
        StateLabel.Text = name;
        TaskLabel.Text = task;
        _tray.Text = ("CAH Sentinel · " + name).Length > 63
            ? "CAH Sentinel" : "CAH Sentinel · " + name;
        _player.SwitchState(animation ?? new AnimationIntent(mood), _settings.SpritePackPath,
            _settings.ReducedMotion, () => _sprites.ForMood(mood));
    }

    private void ShowNotification(string title, string detail, bool important)
    {
        if (!_settings.Monitoring || _exiting) return;
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = detail.Length > 230 ? detail[..227] + "..." : detail;
        _tray.BalloonTipIcon = important ? WinForms.ToolTipIcon.Warning : WinForms.ToolTipIcon.Info;
        _tray.ShowBalloonTip(12000);
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_settings) { Topmost = true };
        if (dialog.ShowDialog() != true) return;
        var changedRepo = !string.Equals(_settings.LocalRepoPath, dialog.Value.LocalRepoPath,
            StringComparison.OrdinalIgnoreCase);
        _generation++;
        _pollCancellation?.Cancel();
        if (changedRepo) _selection.Reset();
        _player.Stop();
        _settings = dialog.Value;
        SettingsStore.Save(_settings);
        Visibility = _settings.ShowPet ? Visibility.Visible : Visibility.Hidden;
        Render(_settings.Monitoring ? PetMood.Idle : PetMood.Paused, "设置已保存");
        UpdateMenu();
        if (_settings.Monitoring) _ = PollAsync();
    }

    private void ToggleMonitoring()
    {
        _generation++;
        _pollCancellation?.Cancel();
        _settings.Monitoring = !_settings.Monitoring;
        SettingsStore.Save(_settings);
        UpdateMenu();
        if (_settings.Monitoring) _ = PollAsync();
        else { _selection.Reset(); Render(PetMood.Paused, "已停止同步与通知"); }
    }

    private void UpdateMenu()
    {
        if (_settings.Monitoring) _timer.Start(); else _timer.Stop();
        _pauseItem.Text = _settings.Monitoring ? "暂停监控（静默）" : "恢复监控";
    }

    private void OpenForeground()
    {
        if (!Uri.TryCreate(_settings.ForegroundUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || uri.Host != "chatgpt.com")
        {
            System.Windows.MessageBox.Show("请先在设置中填写自己的 Foreground ChatGPT 对话链接。", "CAH Sentinel");
            OpenSettings();
            return;
        }
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private void ShowPet()
    {
        _settings.ShowPet = true;
        SettingsStore.Save(_settings);
        Show();
        DesktopPosition.Restore(this, _settings);
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Quit()
    {
        _exiting = true;
        _generation++;
        _pollCancellation?.Cancel();
        _player.Dispose();
        _timer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_exiting) return;
        e.Cancel = true;
        Hide(); // Closing the pet window does not silently end notifications; tray Exit does.
        _settings.ShowPet = false;
        SettingsStore.Save(_settings);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { OpenForeground(); return; }
        try
        {
            DragMove(); // Returns when the mouse is released; persist the final position.
            if (!DesktopPosition.Remember(this, _settings))
                TaskLabel.Text = "位置保存失败，请检查本机配置目录权限";
        }
        catch (InvalidOperationException) { }
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _tray.ContextMenuStrip?.Show(WinForms.Cursor.Position);
    }
}
