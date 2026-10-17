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
