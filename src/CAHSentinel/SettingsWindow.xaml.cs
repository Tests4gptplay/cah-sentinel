using System.IO;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace CAHSentinel;

public partial class SettingsWindow : Window
{
    public SentinelSettings Value { get; private set; }

    public SettingsWindow(SentinelSettings settings)
    {
        InitializeComponent();
        Value = settings;
        LocalPathField.Text = settings.LocalRepoPath;
        ForegroundField.Text = settings.ForegroundUrl;
        NotificationField.IsChecked = settings.Notifications;
        MonitorField.IsChecked = settings.Monitoring;
        PetField.IsChecked = settings.ShowPet;
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        using var picker = new WinForms.FolderBrowserDialog {
            Description = "选择 CAH 的本地 Git 仓库目录（不会访问网络）",
            ShowNewFolderButton = false
        };
        if (Directory.Exists(LocalPathField.Text)) picker.SelectedPath = LocalPathField.Text;
        if (picker.ShowDialog() == WinForms.DialogResult.OK)
            LocalPathField.Text = picker.SelectedPath;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var local = LocalPathField.Text.Trim();
        if (local.Length > 0 &&
            (!Path.IsPathFullyQualified(local) ||
             local.StartsWith(@"\\", StringComparison.Ordinal) ||
             !Directory.Exists(local)))
        {
            System.Windows.MessageBox.Show(this,
                "请选择本机真实存在的 CAH Git 仓库目录，不支持网络共享或 GitHub URL。", "CAH Sentinel");
            return;
        }
        var foreground = ForegroundField.Text.Trim();
        if (foreground.Length > 0 &&
            (!Uri.TryCreate(foreground, UriKind.Absolute, out var target) ||
             target.Scheme != "https" || target.Host != "chatgpt.com"))
        {
            System.Windows.MessageBox.Show(this, "Foreground 必须是 https://chatgpt.com/ 的聊天链接。",
                "CAH Sentinel");
            return;
        }
        Value = new SentinelSettings {
            LocalRepoPath = local, ForegroundUrl = foreground, PollSeconds = 30,
            Notifications = NotificationField.IsChecked == true,
            Monitoring = MonitorField.IsChecked == true,
            ShowPet = PetField.IsChecked == true
        };
        DialogResult = true;
    }
}
