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
        Render(PetMood.Idle, "等待配置");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Right - Width - 22;
        Top = SystemParameters.WorkArea.Bottom - Height - 12;
        Visibility = _settings.ShowPet ? Visibility.Visible : Visibility.Hidden;
        UpdateMenu();
        _timer.Start();
        if (string.IsNullOrWhiteSpace(_settings.LocalRepoPath))
            Dispatcher.BeginInvoke(new Action(OpenSettings));
        else
            await PollAsync();
    }

    private async Task PollAsync()
    {
        if (_polling || !_settings.Monitoring || string.IsNullOrWhiteSpace(_settings.LocalRepoPath))
            return;
        _polling = true;
        try
        {
            // Read only local Git objects already refreshed by CAH; no network fetch or authentication.
            _current = await _reader.ReadAsync(_settings.LocalRepoPath);
            if (!_settings.Monitoring) return;
            _offlineAlerted = false;
            var focus = _current.PriorityTask;
            Render(focus?.Mood ?? PetMood.Idle, focus?.Title ?? "当前没有 Task Cell");
            foreach (var task in _current.Tasks)
            {
                if (!_settings.Notifications) continue;
                var urgent = task.Mood is PetMood.WaitUser or PetMood.Blocked or PetMood.Error or PetMood.Complete;
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
            Render(PetMood.Disconnected, ex is StaleLocalStateException ? "本地状态过期" : "本地状态读取失败");
            if (_settings.Notifications && !_offlineAlerted)
            {
                ShowNotification("CAH Sentinel 本地状态不可用",
                    ex is StaleLocalStateException ? "CAH 的本地状态超过 5 分钟没有刷新，请检查 CAH Bridge。" :
                    "本地 Git 状态读取失败：" + ex.GetType().Name + "。请检查目录与本地 Git。", true);
                _offlineAlerted = true;
            }
        }
        finally { _polling = false; }
    }

    private void Render(PetMood mood, string task)
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
        try
        {
            PetImage.Source = _sprites.ForMood(mood);
            Placeholder.Visibility = PetImage.Source is null ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception)
        {
            PetImage.Source = null;
            Placeholder.Visibility = Visibility.Visible;
        }
    }

    private void ShowNotification(string title, string detail, bool important)
    {
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = detail.Length > 230 ? detail[..227] + "..." : detail;
        _tray.BalloonTipIcon = important ? WinForms.ToolTipIcon.Warning : WinForms.ToolTipIcon.Info;
        _tray.ShowBalloonTip(12000);
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_settings) { Topmost = true };
        if (dialog.ShowDialog() != true) return;
        _settings = dialog.Value;
        SettingsStore.Save(_settings);
        Visibility = _settings.ShowPet ? Visibility.Visible : Visibility.Hidden;
        Render(_settings.Monitoring ? PetMood.Idle : PetMood.Paused, "设置已保存");
        UpdateMenu();
        if (_settings.Monitoring) _ = PollAsync();
    }

    private void ToggleMonitoring()
    {
        _settings.Monitoring = !_settings.Monitoring;
        SettingsStore.Save(_settings);
        UpdateMenu();
        if (_settings.Monitoring) _ = PollAsync();
        else Render(PetMood.Paused, "已停止同步与通知");
    }

    private void UpdateMenu()
    {
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
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Quit()
    {
        _exiting = true;
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
        try { DragMove(); } catch (InvalidOperationException) { }
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _tray.ContextMenuStrip?.Show(WinForms.Cursor.Position);
    }
}
