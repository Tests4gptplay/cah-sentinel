namespace CAHSentinel.Core;

/// <summary>Offline fixed-name pack lookup. No URLs, network shares or symlink escapes.</summary>
public static class AnimationPack
{
    public static string? RelativePath(PetMood mood) => mood switch {
        PetMood.Idle => "states/idle/loop.gif",
        PetMood.Running => "states/working/loop.gif",
        PetMood.WaitUser => "states/wait_user/loop.gif",
        PetMood.Error => "states/error/loop.gif",
        PetMood.Complete => "states/complete/once.gif",
        _ => null
    };

    public static string? Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root)
            || root.StartsWith(@"\\", StringComparison.Ordinal) || root.StartsWith("//", StringComparison.Ordinal)
            || root.Contains("://", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(relative) || relative.Contains('\\') || relative.Contains(':')
            || Path.IsPathRooted(relative)) return null;
        var parts = relative.Split('/');
        if (parts.Any(p => p.Length == 0 || p is "." or ".."
            || !p.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '.'))) return null;
        try
        {
            var fullRoot = Path.GetFullPath(root);
            // Reject reparse points throughout the root and selected file ancestry.
            for (var d = new DirectoryInfo(fullRoot); d is not null; d = d.Parent)
                if ((d.Attributes & FileAttributes.ReparsePoint) != 0) return null;
            var path = fullRoot;
            foreach (var part in parts)
            {
                path = Path.Combine(path, part);
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return null;
            }
            return File.Exists(path) ? path : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        catch (ArgumentException) { return null; }
        catch (NotSupportedException) { return null; }
    }
}
