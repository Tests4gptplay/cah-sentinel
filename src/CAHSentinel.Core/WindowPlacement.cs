namespace CAHSentinel.Core;

public readonly record struct DesktopBounds(int X, int Y, int Width, int Height);
public readonly record struct DesktopPoint(int X, int Y);

/// <summary>Restore saved desktop coordinates, including negative monitor positions.</summary>
public static class WindowPlacement
{
    public static DesktopPoint Restore(int x, int y, int width, int height,
        IReadOnlyList<DesktopBounds> workAreas)
    {
        if (width <= 0 || height <= 0 || workAreas.Count == 0)
            throw new ArgumentException("Window dimensions and monitor work areas are required.");
        // Keep natural placement at monitor edges or across two monitors. Do not snap
        // a deliberately partially offscreen window to an edge on the next startup.
        if (workAreas.Any(a => a.Width > 0 && a.Height > 0
            && Math.Min((long)x + width, (long)a.X + a.Width) - Math.Max((long)x, a.X) >= Math.Min(32, width)
            && Math.Min((long)y + height, (long)a.Y + a.Height) - Math.Max((long)y, a.Y) >= Math.Min(32, height)))
            return new(x, y);
        // Prefer the area requiring the least movement. A removed monitor never leaves
        // the pet stranded offscreen; normal in-bounds placements remain exact.
        var candidates = workAreas.Where(a => a.Width > 0 && a.Height > 0).Select(a => {
            var px = (int)Math.Clamp((long)x, a.X, (long)a.X + Math.Max(0, a.Width - width));
            var py = (int)Math.Clamp((long)y, a.Y, (long)a.Y + Math.Max(0, a.Height - height));
            var dx = (double)x - px;
            var dy = (double)y - py;
            return (Point: new DesktopPoint(px, py), Distance: dx * dx + dy * dy);
        }).OrderBy(p => p.Distance).ToArray();
        if (candidates.Length == 0) throw new ArgumentException("No valid monitor work areas.");
        return candidates[0].Point;
    }
}
