using System.Text.Json;

namespace CAHSentinel.Core;

public enum PetMood { Idle, Running, WaitHelper, WaitUser, Blocked, Error, Complete, Disconnected, Paused }

public sealed record TaskView(
    string Id, string Title, PetMood Mood, string EventKey,
    string Question, string DecisionRef, string Status);

public sealed record TaskSnapshot(string Revision, DateTimeOffset ReadAt, IReadOnlyList<TaskView> Tasks)
{
    public TaskView? PriorityTask => Tasks
        .OrderBy(t => t.Mood switch {
            PetMood.WaitUser => 0, PetMood.Error => 1, PetMood.Blocked => 2,
            PetMood.WaitHelper => 3, PetMood.Running => 4, PetMood.Complete => 5, _ => 6
        }).FirstOrDefault();
}

public static class StateProjection
{
    public static string Value(JsonElement root, params string[] path)
    {
        foreach (var part in path)
        {
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(part, out root))
                return "";
        }
        return root.ValueKind == JsonValueKind.String ? root.GetString() ?? "" : "";
    }

    public static TaskView Project(string taskId, string cellJson, string? taskJson, string? outcomeJson)
    {
        using var cell = JsonDocument.Parse(cellJson);
        var root = cell.RootElement;
        var status = Value(root, "status").ToUpperInvariant();
        var activity = Value(root, "planner_control", "activity").ToUpperInvariant();
        var wait = Value(root, "planner_control", "wait", "kind").ToUpperInvariant();
        var decision = Value(root, "planner_control", "last_decision", "decision_ref");
        var outcomeKind = "";
        var turnSignal = "";
        var question = "";
        if (!string.IsNullOrWhiteSpace(outcomeJson))
        {
            using var outcome = JsonDocument.Parse(outcomeJson);
            outcomeKind = Value(outcome.RootElement, "outcome").ToUpperInvariant();
            turnSignal = Value(outcome.RootElement, "turn_signal").ToUpperInvariant();
            question = Value(outcome.RootElement, "semantic", "question");
            if (string.IsNullOrWhiteSpace(question) && (outcomeKind == "WAIT" || turnSignal == "WAIT"))
                question = Value(outcome.RootElement, "semantic", "wait_condition");
        }
        var title = taskId;
        if (!string.IsNullOrEmpty(taskJson))
        {
            using var task = JsonDocument.Parse(taskJson);
            title = Value(task.RootElement, "title");
            if (string.IsNullOrWhiteSpace(title)) title = taskId;
        }

        var mood = status switch {
            "WAIT" or "WAIT_USER" or "NEED_USER" => PetMood.WaitUser,
            "ERROR" or "FAILURE" => PetMood.Error,
            "BLOCKED" => PetMood.Blocked,
            "DONE" or "SUCCESS" or "COMPLETE_ACCEPTED" => PetMood.Complete,
            _ when activity is "WAIT" or "WAIT_USER" || wait is "WAIT" or "NEED_USER" => PetMood.WaitUser,
            _ when wait == "WAIT_HELPER" || wait == "WAIT_WORKER_HELPER" => PetMood.WaitHelper,
            _ when (outcomeKind is "WAIT" or "NEED_USER" || turnSignal is "WAIT" or "NEED_USER")
                && activity is not ("ACTIVE" or "WAKING" or "REPLANNING")
                && wait is not ("WAIT_RESULT" or "WAIT_RESOURCE" or "WAIT_DEP" or "WAIT_ADMISSION" or "WAIT_INSTALL" or "WAIT_BARRIER") => PetMood.WaitUser,
            _ when activity is "ACTIVE" or "WAKING" or "REPLANNING" => PetMood.Running,
            _ when status == "PARKED_WAIT_EVENT" && activity == "PARKED_WAIT_EVENT" => PetMood.Running,
            _ when status == "RUNNING" || status == "ACTIVE" || status == "NEW" => PetMood.Running,
            _ => PetMood.Idle
        };

        if (mood == PetMood.WaitUser && string.IsNullOrEmpty(question))
            question = "Planner 正在等待人工回复，请打开 Foreground 查看最新问题。";
        // The decision reference is immutable for this Planner turn; event keys suppress repeated alerts.
        var eventKey = taskId + "|" + mood + "|" + (string.IsNullOrWhiteSpace(decision) ? status + ":" + activity : decision);
        return new TaskView(taskId, title, mood, eventKey, question, decision, status);
    }

    public static bool IsSafeGitPath(string path) =>
        !string.IsNullOrWhiteSpace(path) && !path.StartsWith('/') &&
        !path.Contains("..", StringComparison.Ordinal) &&
        path.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '_' or '-' or '.');

    public static bool IsSafeOutcomeRef(string path) =>
        IsSafeGitPath(path) && path.StartsWith("state/planner_turns/", StringComparison.Ordinal)
        && path.EndsWith("/outcome.json", StringComparison.Ordinal);
}
