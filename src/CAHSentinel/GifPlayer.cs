using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CAHSentinel.Core;

namespace CAHSentinel;

/// <summary>One active clip, independent monotonic clock; never reads CAH or the network.</summary>
public sealed class GifPlayer : IDisposable
{
    private readonly Action<BitmapSource?> _display;
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(10) };
    private readonly Stopwatch _clock = new();
    private AnimationTimeline? _timeline;
    private GifClip? _clip;
    private Func<BitmapSource?>? _fallback;
    private (string Pack, PetMood Mood, bool Reduced)? _state;
    private string _event = "";
    private int _frame;
    public bool IsPlaying => _timer.IsEnabled;

    public GifPlayer(Action<BitmapSource?> display)
    {
        _display = display;
        _timer.Tick += OnTick;
    }

    public void SwitchState(AnimationIntent intent, string packRoot, bool reducedMotion,
        Func<BitmapSource?> fallback)
    {
        var state = (packRoot, intent.Mood, reducedMotion);
        // Same-state polls never restart loops or a completed one-shot. A distinct accepted
        // parent event may restart COMPLETE; the no-event poll afterwards must not interrupt it.
        if (_state == state && (!intent.PlayOnce || intent.EventKey == _event)) return;
        Stop();
        _state = state;
        _event = intent.EventKey;
        _fallback = fallback;
        if (intent.Mood is PetMood.Paused or PetMood.Disconnected
            || intent.Mood == PetMood.Complete && !intent.PlayOnce)
        {
            Fallback();
            return;
        }
        var relative = AnimationPack.RelativePath(intent.Mood);
        var path = relative is null ? null : AnimationPack.Resolve(packRoot, relative);
        if (path is null) { Fallback(); return; }
        try
        {
            var clip = GifClip.Load(path);
            _clip = clip;
            _timeline = new AnimationTimeline(clip.Delays);
            _frame = 0;
            _display(clip.Frames[0]);
            if (reducedMotion) return;
            if (intent.PlayOnce) PlayOnce(); else PlayLoop();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { Fallback(); }
    }

    public void PlayLoop()
    {
        if (_timeline is null) return;
        _timeline.PlayLoop();
        _clock.Restart();
        _timer.Start();
    }

    public void PlayOnce()
    {
        if (_timeline is null) return;
        _timeline.PlayOnce(); // Deliberately ignores any GIF loop extension.
        _clock.Restart();
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_timeline is null || _clip is null) return;
        var frame = _timeline.Advance(_clock.Elapsed);
        if (!_timeline.IsPlaying) { Fallback(); return; }
        if (frame == _frame) return;
        _frame = frame;
        _display(_clip.Frames[frame]);
    }

    private void Fallback()
    {
        _timer.Stop();
        _clock.Stop();
        _timeline?.Stop();
        _timeline = null;
        _clip = null;
        BitmapSource? image = null;
        try { image = _fallback?.Invoke(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        _display(image);
    }

    public void Stop()
    {
        _timer.Stop();
        _clock.Reset();
        _timeline?.Stop();
        _timeline = null;
        _clip = null;
        _state = null;
        _event = "";
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= OnTick;
    }
}
