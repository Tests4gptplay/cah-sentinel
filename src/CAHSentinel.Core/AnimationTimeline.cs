namespace CAHSentinel.Core;

/// <summary>Elapsed-time playback, independent of polling and GIF loop extensions.</summary>
public sealed class AnimationTimeline
{
    private readonly long[] _ends;
    public TimeSpan Duration { get; }
    public int FrameIndex { get; private set; }
    public bool IsPlaying { get; private set; }
    private bool _loop;

    public AnimationTimeline(IEnumerable<TimeSpan> delays)
    {
        var total = 0L;
        _ends = delays.Select(d => {
            if (d <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delays));
            return total = checked(total + d.Ticks);
        }).ToArray();
        if (_ends.Length == 0) throw new ArgumentException("At least one frame is required.");
        Duration = TimeSpan.FromTicks(total);
    }

    public void PlayLoop() { _loop = true; FrameIndex = 0; IsPlaying = true; }
    public void PlayOnce() { _loop = false; FrameIndex = 0; IsPlaying = true; }
    public void Stop() => IsPlaying = false;

    public int Advance(TimeSpan elapsed)
    {
        if (!IsPlaying) return FrameIndex;
        var ticks = Math.Max(0, elapsed.Ticks);
        if (!_loop && ticks >= Duration.Ticks)
        {
            IsPlaying = false;
            return FrameIndex = _ends.Length - 1;
        }
        if (_loop) ticks %= Duration.Ticks;
        FrameIndex = Array.FindIndex(_ends, end => ticks < end);
        return FrameIndex;
    }
}
