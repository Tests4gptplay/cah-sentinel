using CAHSentinel.Core;
using System.Text.Json;

var tests = 0;
void Check(bool pass, string label)
{
    tests++;
    if (!pass) throw new Exception("FAILED: " + label);
}
const string outcome = """
{"outcome":"NEED_USER","semantic":{"question":"Please inspect your antivirus."}}
""";
const string title = """{"title":"MV example"}""";
TaskView Parse(string status, string activity, string waitKind, string decision = "state/planner_turns/x/outcome.json")
{
    var cellJson = JsonSerializer.Serialize(new {
        status,
        planner_control = new {
            activity, wait = new { kind = waitKind },
            last_decision = new { decision_ref = decision }
        }
    });
    return StateProjection.Project("example", cellJson, title, outcome);
}

var a = Parse("WAIT_USER", "WAIT_USER", "NEED_USER");
Check(a.Mood == PetMood.WaitUser, "WAIT_USER mapped");
Check(a.Question.Contains("antivirus", StringComparison.Ordinal), "question parsed");
Check(a.EventKey.Contains("state/planner_turns/x/outcome.json"), "stable event key");
Check(Parse("PARKED_WAIT_EVENT", "PARKED_WAIT_EVENT", "WAIT_HELPER").Mood == PetMood.WaitHelper, "helper");
Check(Parse("RUNNING", "WAKING", "").Mood == PetMood.Running, "running");
Check(Parse("DONE", "DONE", "").Mood == PetMood.Complete, "completed");
Check(Parse("ERROR", "ERROR", "").Mood == PetMood.Error, "error");
Check(Parse("BLOCKED", "ACTIVE", "").Mood == PetMood.Blocked, "blocked");
Check(StateProjection.IsSafeOutcomeRef("state/planner_turns/a/outcome.json"), "safe outcome");
Check(!StateProjection.IsSafeOutcomeRef("../../secret"), "path traversal rejected");
Check(!StateProjection.IsSafeOutcomeRef("tasks/x.json"), "untrusted outcome rejected");
var b = Parse("WAIT_USER", "WAIT_USER", "NEED_USER", "state/planner_turns/y/outcome.json");
Check(a.EventKey != b.EventKey, "new question triggers notification");
var list = new TaskSnapshot("abc", DateTimeOffset.UtcNow,
    [Parse("RUNNING", "ACTIVE", ""), a, Parse("ERROR", "ACTIVE", "")]);
Check(list.PriorityTask?.Mood == PetMood.WaitUser, "urgent alert priority");
Console.WriteLine("CAH Sentinel core tests PASSED: " + tests);

await TestLocalReaderAsync();
Console.WriteLine("CAH Sentinel local-only Git fixture tests PASSED");

static async Task TestLocalReaderAsync()
{
    var temp = Path.Combine(Path.GetTempPath(), "sentinel-local-test-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(temp);
    try
    {
        async Task<string> Git(params string[] args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git") {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(temp);
            foreach (var arg in args) psi.ArgumentList.Add(arg);
            using var p = System.Diagnostics.Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEndAsync();
            var stderr = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            var value = await stdout;
            var error = await stderr;
            if (p.ExitCode != 0) throw new Exception("Fixture git failure: " + error);
            return value.Trim();
        }
        Directory.CreateDirectory(Path.Combine(temp, "state", "task_cells"));
        Directory.CreateDirectory(Path.Combine(temp, "state", "planner_turns", "test"));
        Directory.CreateDirectory(Path.Combine(temp, "tasks"));
        File.WriteAllText(Path.Combine(temp, "state", "chatgpt.json"), "{}");
        File.WriteAllText(Path.Combine(temp, "state", "task_cells", "test.json"),
            """{"status":"WAIT_USER","planner_control":{"activity":"WAIT_USER","wait":{"kind":"NEED_USER"},"last_decision":{"decision_ref":"state/planner_turns/test/outcome.json"}}}""");
        File.WriteAllText(Path.Combine(temp, "state", "planner_turns", "test", "outcome.json"),
            """{"semantic":{"question":"Please review local status."}}""");
        File.WriteAllText(Path.Combine(temp, "tasks", "test.json"), """{"title":"Local test"}""");
        await Git("init");
        await Git("add", ".");
        await Git("-c", "user.name=Sentinel Test", "-c", "user.email=test@example.invalid",
            "commit", "-m", "Synthetic local-only task state");
        await Git("fetch", ".", "HEAD"); // Fixture setup only: no Sentinel runtime remote command.

        var reader = new LocalStateReader();
        var snapshot = await reader.ReadAsync(temp);
        if (snapshot.Tasks.Count != 1 || snapshot.PriorityTask?.Mood != PetMood.WaitUser
            || snapshot.PriorityTask?.Question != "Please review local status.")
            throw new Exception("Local FETCH_HEAD Task Cell projection failed");
        if (snapshot.Revision.Length != 40) throw new Exception("Invalid local revision");
        var fetchPath = Path.Combine(temp, ".git", "FETCH_HEAD");
        File.SetLastWriteTimeUtc(fetchPath, DateTime.UtcNow.AddMinutes(-20));
        try { await reader.ReadAsync(temp); throw new Exception("Stale FETCH_HEAD incorrectly accepted"); }
        catch (StaleLocalStateException) { }
        File.Delete(fetchPath);
        try { await reader.ReadAsync(temp); throw new Exception("Missing FETCH_HEAD incorrectly accepted"); }
        catch (StaleLocalStateException) { }
        try { await reader.ReadAsync("https://example.com/private/repo.git"); throw new Exception("Network URL accepted as local root"); }
        catch (ArgumentException) { }
    }
    finally
    {
        try
        {
            // Git for Windows may mark pack/object files readonly; normalize only the disposable fixture.
            foreach (var file in Directory.EnumerateFiles(temp, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(temp, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
