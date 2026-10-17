using System.Diagnostics;
using System.Text;

namespace CAHSentinel.Core;

/// <summary>Reads Git canonically through a private mirror. Never writes to the CAH checkout or task state.</summary>
public sealed class GitMirrorReader
{
    private readonly string _dir;
    public GitMirrorReader(string localDataDirectory) =>
        _dir = Path.Combine(localDataDirectory, "readonly-mirror.git");

    public async Task<TaskSnapshot> FetchAsync(string remoteUrl, string branch, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Enter an HTTPS Git URL without a password or access token.");
        if (string.IsNullOrWhiteSpace(branch) ||
            !branch.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '_' or '-' or '.')
            || branch.Contains("..", StringComparison.Ordinal) || branch.StartsWith('-'))
            throw new ArgumentException("Invalid Git branch.");

        Directory.CreateDirectory(Path.GetDirectoryName(_dir)!);
        if (!File.Exists(Path.Combine(_dir, "HEAD")))
            await ExecuteAsync(cancellationToken, "init", "--bare", _dir);

        string? savedRemote;
        try { savedRemote = (await GitAsync(cancellationToken, "remote", "get-url", "origin")).Trim(); }
        catch (InvalidOperationException) { savedRemote = null; }

        if (savedRemote == null)
            await GitAsync(cancellationToken, "remote", "add", "origin", remoteUrl);
        else if (!string.Equals(savedRemote, remoteUrl, StringComparison.Ordinal))
            await GitAsync(cancellationToken, "remote", "set-url", "origin", remoteUrl);

        await GitAsync(cancellationToken, "fetch", "--depth=1", "--no-tags", "--quiet", "origin",
            "+refs/heads/" + branch + ":refs/remotes/origin/" + branch);
        var revision = (await GitAsync(cancellationToken, "rev-parse", "refs/remotes/origin/" + branch)).Trim();
        var paths = (await GitAsync(cancellationToken, "ls-tree", "-r", "--name-only", revision,
            "state/task_cells")).Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x =>
                x.StartsWith("state/task_cells/", StringComparison.Ordinal) &&
                x.EndsWith(".json", StringComparison.Ordinal) && StateProjection.IsSafeGitPath(x))
            .Take(50).ToArray();
        var all = new List<TaskView>();
        foreach (var path in paths)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var cellJson = await GitAsync(cancellationToken, "show", revision + ":" + path);
            using var cell = System.Text.Json.JsonDocument.Parse(cellJson);
            var outcomeRef = StateProjection.Value(cell.RootElement,
                "planner_control", "last_decision", "decision_ref");
            var titleRef = "tasks/" + id + ".json";
            string? taskJson = await OptionalFileAsync(cancellationToken, revision, titleRef);
            string? outcomeJson = StateProjection.IsSafeOutcomeRef(outcomeRef)
                ? await OptionalFileAsync(cancellationToken, revision, outcomeRef) : null;
            all.Add(StateProjection.Project(id, cellJson, taskJson, outcomeJson));
        }
        return new TaskSnapshot(revision, DateTimeOffset.UtcNow, all);
    }

    private async Task<string?> OptionalFileAsync(CancellationToken ct, string sha, string path)
    {
        if (!StateProjection.IsSafeGitPath(path)) return null;
        try { return await GitAsync(ct, "show", sha + ":" + path); }
        catch (InvalidOperationException) { return null; }
    }

    private Task<string> GitAsync(CancellationToken ct, params string[] args) =>
        ExecuteAsync(ct, new[] { "--git-dir=" + _dir }.Concat(args).ToArray());

    private static async Task<string> ExecuteAsync(CancellationToken ct, params string[] args)
    {
        var info = new ProcessStartInfo("git")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var arg in args) info.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = info };
        if (!process.Start()) throw new InvalidOperationException("Unable to start git.exe.");
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("Git read timed out. Check network and Git credentials.");
        }
        var output = await stdout;
        var error = await stderr;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                "Git read failed (exit " + process.ExitCode + "). Ensure git.exe works and the private repository is accessible. " +
                // Do not echo potentially credential-bearing remote URLs or Git command lines.
                (error.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase)
                    ? "Authentication failed." : "See the Git credential configuration."));
        return output;
    }
}
