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
Check(Parse("WAIT", "IDLE", "").Mood == PetMood.WaitUser, "legacy WAIT status maps to NEED_USER");
Check(Parse("PARKED_WAIT_EVENT", "WAIT", "").Mood == PetMood.WaitUser, "legacy WAIT activity maps to NEED_USER");
Check(Parse("PARKED_WAIT_EVENT", "PARKED_WAIT_EVENT", "WAIT").Mood == PetMood.WaitUser, "legacy WAIT kind maps to NEED_USER");
const string legacyWaitOutcome = """{"outcome":"WAIT","turn_signal":"wait","semantic":{"wait_condition":"Please choose the next step."}}""";
const string parkedCell = """{"status":"PARKED_WAIT_EVENT","planner_control":{"activity":"PARKED_WAIT_EVENT","wait":null,"last_decision":{"decision_ref":"state/planner_turns/legacy/outcome.json"}}}""";
var legacy = StateProjection.Project("legacy", parkedCell, title, legacyWaitOutcome);
Check(legacy.Mood == PetMood.WaitUser, "Planner WAIT outcome maps to NEED_USER");
Check(legacy.Question == "Please choose the next step.", "legacy WAIT condition appears as user question");
const string helperCell = """{"status":"PARKED_WAIT_EVENT","planner_control":{"activity":"PARKED_WAIT_EVENT","wait":{"kind":"WAIT_HELPER"}}}""";
Check(StateProjection.Project("helper", helperCell, title, legacyWaitOutcome).Mood == PetMood.WaitHelper, "WAIT_HELPER remains distinct");
Check(Parse("PARKED_WAIT_EVENT", "PARKED_WAIT_EVENT", "WAIT_HELPER").Mood == PetMood.WaitHelper, "helper");
foreach(var activity in new[] { "ACTIVE", "WAKING", "REPLANNING" })
    Check(Parse("PARKED_WAIT_EVENT", activity, "").Mood == PetMood.Running, "active Planner while parent parked: " + activity);
Check(Parse("PARKED_WAIT_EVENT", "WAKING", "NEED_USER").Mood == PetMood.WaitUser, "user wait takes precedence");
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
// Elapsed-time clock: exact boundary, delayed dispatcher, loop and forced once.
var clock = new AnimationTimeline([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(250)]);
clock.PlayLoop();
Check(clock.Advance(TimeSpan.FromMilliseconds(99)) == 0, "first frame delay");
Check(clock.Advance(TimeSpan.FromMilliseconds(100)) == 1, "exact frame boundary");
Check(clock.Advance(TimeSpan.FromMilliseconds(750)) == 0, "late dispatcher catches up across loops");
clock.PlayOnce();
Check(clock.Advance(TimeSpan.FromMilliseconds(349)) == 1 && clock.IsPlaying, "once final hold");
Check(clock.Advance(TimeSpan.FromMilliseconds(350)) == 1 && !clock.IsPlaying, "once cannot loop");
clock.Stop();
Check(!clock.IsPlaying, "stop clock");

TaskSnapshot Snapshot(params TaskView[] items) => new("test", DateTimeOffset.UtcNow, items);
var selector = new AnimationSelection();
var seen = new HashSet<string>();
var accepted = Parse("COMPLETE_ACCEPTED", "DONE", "");
var running = Parse("RUNNING", "ACTIVE", "");
Check(!selector.Observe(Snapshot(accepted), seen).PlayOnce, "history never celebrates at startup");
Check(!selector.Observe(Snapshot(accepted), seen).PlayOnce, "history stays deduplicated");
selector.Reset(); seen.Clear();
selector.Observe(Snapshot(running), seen);
Check(selector.Observe(Snapshot(accepted), seen).PlayOnce, "new parent acceptance celebrates once");
Check(!selector.Observe(Snapshot(accepted), seen).PlayOnce, "poll cannot restart celebration");
selector.Reset(); seen.Clear(); selector.Observe(Snapshot(running), seen);
Check(!selector.Observe(Snapshot(Parse("DONE", "DONE", "")), seen).PlayOnce, "generic DONE is not acceptance");
Check(!selector.Observe(Snapshot(Parse("SUCCESS", "DONE", "")), seen).PlayOnce, "generic SUCCESS is not acceptance");
selector.Reset(); seen.Clear(); selector.Observe(Snapshot(running), seen);
var urgent = a with { Id = "urgent", EventKey = "urgent-wait" };
Check(selector.Observe(Snapshot(accepted, urgent), seen).Mood == PetMood.WaitUser, "WAIT_USER preempts COMPLETE");
Check(!selector.Observe(Snapshot(accepted), seen).PlayOnce, "preempted completion never replays later");
Check(selector.Observe(Snapshot(urgent), seen).Mood == PetMood.WaitUser, "wait remains until durable state clears");
Check(selector.Observe(Snapshot(Parse("ERROR", "ERROR", "")), seen).Mood == PetMood.Error, "error preempts");
Check(selector.Observe(Snapshot(Parse("ERROR", "ERROR", "")), seen).Mood == PetMood.Error, "error cannot self-recover");
selector.Reset();
Check(!selector.Observe(Snapshot(accepted), new HashSet<string>()).PlayOnce, "resume or reconnect rebaselines");

var pack = Path.Combine(Path.GetTempPath(), "sentinel-pack-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(pack, "states", "idle"));
try
{
    var file = Path.Combine(pack, "states", "idle", "loop.gif");
    File.WriteAllText(file, "synthetic path lookup only");
    Check(AnimationPack.Resolve(pack, "states/idle/loop.gif") == file, "offline pack resolution");
    foreach (var unsafePath in new[] { "../secret.gif", "/absolute.gif", "states/../secret.gif",
        "https://example.com/image.gif", "states\\idle\\loop.gif", "states//idle/loop.gif", "states/idle/loop.gif:stream" })
        Check(AnimationPack.Resolve(pack, unsafePath) is null, "unsafe pack path rejected: " + unsafePath);
    Check(AnimationPack.Resolve("https://example.com", "loop.gif") is null, "remote pack rejected");
    Check(AnimationPack.Resolve(@"\\server\share", "loop.gif") is null, "UNC pack rejected");
    Check(AnimationPack.Resolve(pack, "states/error/missing.gif") is null, "missing GIF fallback");
    Check(AnimationPack.RelativePath(PetMood.Complete) == "states/complete/once.gif", "complete uses once media");
    Check(AnimationPack.RelativePath(PetMood.Paused) is null, "pause has static fallback");
    Check(AnimationPack.RelativePath(PetMood.Disconnected) is null, "stale has static fallback");
    if (!OperatingSystem.IsWindows())
    {
        var link = Path.Combine(pack, "states", "link");
        Directory.CreateSymbolicLink(link, Path.Combine(pack, "states", "idle"));
        Check(AnimationPack.Resolve(pack, "states/link/loop.gif") is null, "symlink pack escape rejected");
        Directory.Delete(link);
    }
}
finally { Directory.Delete(pack, recursive: true); }
// Saved placement survives restart, screen edges, left-side monitors and unplugging.
var desktop = new[] { new DesktopBounds(0, 0, 1920, 1040), new DesktopBounds(-1280, 0, 1280, 984) };
Check(WindowPlacement.Restore(350, 420, 280, 350, desktop) == new DesktopPoint(350, 420), "restore exact dropped position");
Check(WindowPlacement.Restore(-1100, 200, 280, 350, desktop) == new DesktopPoint(-1100, 200), "negative monitor coordinates retained");
Check(WindowPlacement.Restore(-100, 100, 280, 350, desktop) == new DesktopPoint(-100, 100), "spanning monitor boundary does not snap");
Check(WindowPlacement.Restore(1850, 900, 280, 350, desktop) == new DesktopPoint(1850, 900), "partially visible edge placement does not snap");
Check(WindowPlacement.Restore(4000, 300, 280, 350, desktop) == new DesktopPoint(1640, 300), "removed right monitor recovered");
Check(WindowPlacement.Restore(-1100, 200, 280, 350, [desktop[0]]) == new DesktopPoint(0, 200), "removed left monitor recovered");
Check(WindowPlacement.Restore(100, 3000, 280, 350, desktop) == new DesktopPoint(100, 690), "offscreen vertical position recovered");
Check(WindowPlacement.Restore(int.MaxValue, int.MinValue, 280, 350, [desktop[0]]) == new DesktopPoint(1640, 0), "invalid extreme saved coordinates recovered without overflow");
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
