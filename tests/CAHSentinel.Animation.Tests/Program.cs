using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CAHSentinel;
using CAHSentinel.Core;

internal static class Program
{
    private static int _checks;
    private static void Check(bool pass, string label)
    {
        if (!pass) throw new Exception("FAILED: " + label);
        _checks++;
    }

    [STAThread]
    private static void Main()
    {
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "disposal.gif");
        var clip = GifClip.Load(fixture);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(fixture, "expected.json")));
        Check(clip.Frames.Count == 4, "four synthetic GIF frames decoded");
        for (var i = 0; i < clip.Frames.Count; i++)
        {
            var frame = clip.Frames[i];
            Check(frame.PixelWidth == 3 && frame.PixelHeight == 2, "full canvas preserved");
            var actual = new byte[3 * 2 * 4];
            frame.CopyPixels(actual, 3 * 4, 0);
            var oracle = expected.RootElement[i];
            Check(actual.SequenceEqual(oracle.GetProperty("pixels_bgra").EnumerateArray().Select(n => n.GetByte())),
                "pixel composition, transparency and disposal frame " + i);
            Check(clip.Delays[i].TotalMilliseconds == oracle.GetProperty("delay_ms").GetInt32(), "exact delay " + i);
        }

        // Read archived candidates from the checkout for technical decode only. Never copy
        // these unreviewed assets into application/test build or release output.
        var candidates = Path.Combine(Environment.CurrentDirectory, "assets", "animation-candidates");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(candidates, "manifest.v0.4.json")));
        foreach (var asset in manifest.RootElement.GetProperty("assets").EnumerateArray())
        {
            var candidate = GifClip.Load(Path.Combine(candidates, asset.GetProperty("path").GetString()!));
            Check(candidate.Frames.Count == asset.GetProperty("frame_count").GetInt32(), "candidate frame count");
            Check(candidate.Delays.Sum(t => t.TotalMilliseconds) == asset.GetProperty("run_ms").GetInt32(), "candidate timing");
            Check(candidate.Frames.All(f => f.PixelWidth == 420 && f.PixelHeight == 420), "candidate canvas");
        }

        var root = Path.Combine(Path.GetTempPath(), "sentinel-animation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (var mood in new[] { PetMood.Idle, PetMood.Running, PetMood.WaitHelper, PetMood.WaitUser, PetMood.Error, PetMood.Complete })
            {
                var dest = Path.Combine(root, AnimationPack.RelativePath(mood)!);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(fixture, dest);
            }
            var fallback = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
                new byte[] { 255, 0, 255, 255 }, 4);
            fallback.Freeze();
            BitmapSource? displayed = null;
            var displays = 0;
            using var player = new GifPlayer(image => { displayed = image; displays++; });
            player.SwitchState(new(PetMood.Running), root, false, () => fallback);
            Check(player.IsPlaying && displayed?.PixelWidth == 3, "local animation starts");
            Pump(TimeSpan.FromMilliseconds(160));
            Check(displays >= 2, "dispatcher independently advances real frames");
            var prior = displays;
            player.SwitchState(new(PetMood.Running), root, false, () => fallback);
            Check(displays == prior, "poll does not reload or restart loop");
            player.SwitchState(new(PetMood.WaitHelper), root, false, () => fallback);
            Check(player.IsPlaying && displayed?.PixelWidth == 3, "Helper uses custom-pack GIF instead of placeholder");
            player.SwitchState(new(PetMood.WaitHelper), Path.Combine(root, "missing-pack"), false, () => fallback);
            Check(player.IsPlaying && displayed?.PixelWidth == 420, "Helper uses bundled GIF when custom pack has no Helper");
            player.SwitchState(new(PetMood.WaitUser), root, false, () => fallback);
            Check(player.IsPlaying, "urgent WAIT_USER preempts running");
            player.SwitchState(new(PetMood.Paused), root, false, () => fallback);
            prior = displays;
            Pump(TimeSpan.FromMilliseconds(150));
            Check(!player.IsPlaying && displayed == fallback && displays == prior, "pause stops frame callbacks");
            player.SwitchState(new(PetMood.Complete, "parent-acceptance-1", true), root, false, () => fallback);
            Pump(TimeSpan.FromMilliseconds(1100));
            Check(!player.IsPlaying && displayed == fallback, "COMPLETE stops despite infinite GIF loop extension");
            prior = displays;
            player.SwitchState(new(PetMood.Complete, "parent-acceptance-1", true), root, false, () => fallback);
            player.SwitchState(new(PetMood.Complete), root, false, () => fallback);
            Check(!player.IsPlaying && displays == prior, "same parent event and no-event polls cannot replay");
            player.SwitchState(new(PetMood.Complete, "parent-acceptance-2", true), root, false, () => fallback);
            Check(player.IsPlaying, "distinct parent acceptance may play again");
            player.SwitchState(new(PetMood.Error), root, false, () => fallback);
            Check(player.IsPlaying, "ERROR immediately preempts celebration");
            player.SwitchState(new(PetMood.Disconnected), root, false, () => fallback);
            Check(!player.IsPlaying && displayed == fallback, "stale state stops success imagery");
            player.SwitchState(new(PetMood.Idle), root, true, () => fallback);
            Check(!player.IsPlaying && displayed?.PixelWidth == 3, "reduced motion shows frozen first frame");
            player.Stop();
            player.SwitchState(new(PetMood.Idle), Path.Combine(root, "missing"), false, () => fallback);
            Check(!player.IsPlaying && displayed == fallback, "missing pack falls back");
            File.WriteAllText(Path.Combine(root, AnimationPack.RelativePath(PetMood.Running)!), "not a GIF");
            player.SwitchState(new(PetMood.Running), root, false, () => fallback);
            Check(!player.IsPlaying && displayed == fallback, "corrupt local media falls back");
            player.Stop();
            player.SwitchState(new(PetMood.Running), root, false, () => throw new InvalidDataException());
            Check(displayed is null, "bad GIF and bad PNG use placeholder");
            var oversized = Path.Combine(root, "oversized.gif");
            File.WriteAllBytes(oversized, [71, 73, 70, 56, 57, 97, 1, 4, 1, 0, 0, 0, 0]);
            try { GifClip.Load(oversized); throw new Exception("Oversized GIF accepted"); }
            catch (InvalidDataException ex) { Check(ex.Message == "GIF canvas limit.", "canvas rejected before native decode"); }
            var truncated = Path.Combine(root, "truncated.gif");
            File.WriteAllBytes(truncated, File.ReadAllBytes(fixture)[..^1]);
            try { GifClip.Load(truncated); throw new Exception("Truncated GIF accepted"); }
            catch (EndOfStreamException) { Check(true, "truncated sub-block/trailer rejected before native decode"); }
        }
        finally { Directory.Delete(root, recursive: true); }
        Console.WriteLine("CAH Sentinel Windows GIF tests PASSED: " + _checks);
    }

    private static void Pump(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
}
