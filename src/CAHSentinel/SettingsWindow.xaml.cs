using System.Windows;

namespace CAHSentinel;

public partial class SettingsWindow : Window
{
    public SentinelSettings Value { get; private set; }
    public SettingsWindow(SentinelSettings settings)
    {
        InitializeComponent();
        Value = settings;
        RemoteField.Text = settings.GitRemote;
        BranchField.Text = settings.Branch;
        ForegroundField.Text = settings.ForegroundUrl;
        NotificationField.IsChecked = settings.Notifications;
        MonitorField.IsChecked = settings.Monitoring;
        PetField.IsChecked = settings.ShowPet;
    }
    private void OnSave(object sender, RoutedEventArgs e)
    {
        var remote = RemoteField.Text.Trim();
        if (remote.Length > 0 && (!Uri.TryCreate(remote, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo)))
        {
            MessageBox.Show(this, "请输入不含 Token 或密码的 HTTPS Git 仓库 URL。", "CAH Sentinel");
            return;
        }
        var branch = BranchField.Text.Trim();
        if (branch.Length == 0 || branch.StartsWith('-') || branch.Contains("..", StringComparison.Ordinal)
            || !branch.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '/' or '_' or '-' or '.'))
        {
            MessageBox.Show(this, "分支名无效。", "CAH Sentinel");
            return;
        }
        var url = ForegroundField.Text.Trim();
        if (url.Length > 0 && (!Uri.TryCreate(url, UriKind.Absolute, out var target) ||
            target.Scheme != "https" || target.Host != "chatgpt.com"))
        {
            MessageBox.Show(this, "Foreground 必须是 https://chatgpt.com/ 下的聊天链接。", "CAH Sentinel");
            return;
        }
        Value = new SentinelSettings {
            GitRemote = remote, Branch = branch, ForegroundUrl = url,
            PollSeconds = 30, Notifications = NotificationField.IsChecked == true,
            Monitoring = MonitorField.IsChecked == true, ShowPet = PetField.IsChecked == true
        };
        DialogResult = true;
    }
}
