namespace CAHSentinel.Core;

public sealed record AnimationIntent(PetMood Mood, string EventKey = "", bool PlayOnce = false);

/// <summary>Conservative parent Task Cell acceptance gate; Worker DONE never celebrates.</summary>
public sealed class AnimationSelection
{
    private readonly Dictionary<string, TaskView> _previous = new(StringComparer.Ordinal);
    private readonly HashSet<string> _acceptedEvents = new(StringComparer.Ordinal);
    private bool _baselined;

    public void Reset()
    {
        _previous.Clear();
        _acceptedEvents.Clear();
        _baselined = false;
    }

    public AnimationIntent Observe(TaskSnapshot snapshot, ISet<string> seen)
    {
        TaskView? celebration = null;
        foreach (var task in snapshot.Tasks)
        {
            // Only the explicit parent Task Cell COMPLETE_ACCEPTED is proof of acceptance.
            // Generic DONE/SUCCESS and outcome-only Worker/Planner signals are insufficient.
            if (task.Status == "COMPLETE_ACCEPTED" && task.Mood == PetMood.Complete)
            {
                var key = "accepted|" + task.EventKey;
                var fresh = _acceptedEvents.Add(key) && !seen.Contains(key);
                if (_baselined && fresh && _previous.TryGetValue(task.Id, out var prior)
                    && prior.Mood != PetMood.Complete)
                    celebration ??= task;
                seen.Add(key); // Includes first-sync history and suppressed urgent events.
            }
        }
        _previous.Clear();
        foreach (var task in snapshot.Tasks) _previous[task.Id] = task;
        _baselined = true;
        var focus = snapshot.PriorityTask;
        if (focus?.Mood is PetMood.WaitUser or PetMood.Error or PetMood.Blocked or PetMood.WaitHelper)
            return new(focus.Mood);
        if (celebration is not null)
            return new(PetMood.Complete, celebration.EventKey, PlayOnce: true);
        return new(focus?.Mood ?? PetMood.Idle);
    }
}
