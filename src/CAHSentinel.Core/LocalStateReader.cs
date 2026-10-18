using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CAHSentinel.Core;

/// <summary>Purely local, read-only CAH Git state reader. Never fetches or changes refs.</summary>
public sealed class LocalStateReader
{
    public const int StaleAfterMinutes = 5;

    public async Task<TaskSnapshot> ReadAsync(string localRepoRoot, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(localRepoRoot) || !Path.IsPathFullyQualified(localRepoRoot)
            || localRepoRoot.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("Select a fully qualified local CAH repository folder (not a URL or network share).");

        var root = Path.GetFullPath(localRepoRoot);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Local CAH folder not found.");

        var top = (await GitAsync(root, ct, "rev-parse", "--show-toplevel")).Trim();
        if (!string.Equals(Path.GetFullPath(top).TrimEnd(Path.DirectorySeparatorChar),
            root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Select the CAH repository ROOT, not a subdirectory.");

        // The existing CAH Bridge refreshes its local FETCH_HEAD. A working-tree JSON file
        // may be stale even when the live canonical Task Cell has advanced.
        var fetchPath = (await GitAsync(root, ct, "rev-parse", "--git-path", "FETCH_HEAD")).Trim();
        if (!Path.IsPathRooted(fetchPath)) fetchPath = Path.GetFullPath(Path.Combine(root, fetchPath));
        if (!File.Exists(fetchPath)) throw new StaleLocalStateException("Local FETCH_HEAD is missing; CAH has not synchronized its status yet.");

        var syncedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(fetchPath));
        var age = DateTimeOffset.UtcNow - syncedAt;
        if (age > TimeSpan.FromMinutes(StaleAfterMinutes) || age < TimeSpan.FromMinutes(-2))
            throw new StaleLocalStateException("The local CAH status snapshot is outdated; no fresh state is available.");

        var revision = (await GitAsync(root, ct, "rev-parse", "--verify", "FETCH_HEAD^{commit}")).Trim();
        var hasState = (await GitAsync(root, ct, "ls-tree", revision, "--", "state/chatgpt.json")).Trim();
        if (hasState.Length == 0)
            throw new InvalidDataException("The selected folder does not contain CAH state at its local FETCH_HEAD.");

        var paths = (await GitAsync(root, ct, "ls-tree", "-r", "--name-only",
                revision, "--", "state/task_cells"))
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x =>
                x.StartsWith("state/task_cells/", StringComparison.Ordinal) &&
                x.EndsWith(".json", StringComparison.Ordinal) && StateProjection.IsSafeGitPath(x))
            .Take(100).ToArray();

        var tasks = new List<TaskView>();
        foreach (var path in paths)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var cellJson = await GitAsync(root, ct, "show", revision + ":" + path);
            using var cell = JsonDocument.Parse(cellJson);
            var decisionRef = StateProjection.Value(cell.RootElement,
                "planner_control", "last_decision", "decision_ref");
            var task = await OptionalFileAsync(root, revision, "tasks/" + id + ".json", ct);
            var decision = StateProjection.IsSafeOutcomeRef(decisionRef)
                ? await OptionalFileAsync(root, revision, decisionRef, ct) : null;
            tasks.Add(StateProjection.Project(id, cellJson, task, decision));
        }
        return new TaskSnapshot(revision, DateTimeOffset.UtcNow, tasks);
    }

    private static async Task<string?> OptionalFileAsync(string root, string sha, string path, CancellationToken ct)
    {
        if (!StateProjection.IsSafeGitPath(path)) return null;
        try { return await GitAsync(root, ct, "show", sha + ":" + path); }
        catch (InvalidOperationException) { return null; }
    }

    private static async Task<string> GitAsync(string root, CancellationToken ct, params string[] args)
    {
        var info = new ProcessStartInfo("git")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        info.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        info.ArgumentList.Add("-C");
        info.ArgumentList.Add(root);
        foreach (var arg in args) info.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = info };
        if (!process.Start()) throw new InvalidOperationException("Unable to run local Git.");
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("Local Git read timed out.");
        }
        var output = await stdout;
        await stderr;
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Unable to read local Git object (exit " +
                process.ExitCode + "). Check the CAH folder and local status availability.");
        return output;
    }
}

public sealed class StaleLocalStateException(string message) : InvalidOperationException(message);
