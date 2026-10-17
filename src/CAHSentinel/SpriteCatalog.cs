using System.Windows;
using System.Windows.Media.Imaging;
using CAHSentinel.Core;

namespace CAHSentinel;

/// <summary>Exact, lossless crop from the user's original RGBA pose sheets.</summary>
public sealed class SpriteCatalog
{
    private readonly Dictionary<string, BitmapSource> _sheets = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _dir = Path.Combine(AppContext.BaseDirectory, "assets", "sprites");

    public BitmapSource? ForMood(PetMood mood)
    {
        var (sheet, count, frame) = mood switch
        {
            PetMood.Idle => ("pet-idle.png", 6, 0),
            PetMood.Running => ("pet-work.png", 6, 1),
            PetMood.WaitUser => ("pet-work.png", 6, 4),
            PetMood.WaitHelper => ("pet-rest.png", 8, 3),
            PetMood.Blocked => ("pet-idle.png", 6, 2),
            PetMood.Error => ("pet-rest.png", 8, 7),
            PetMood.Complete => ("pet-work.png", 6, 5),
            PetMood.Disconnected => ("pet-rest.png", 8, 4),
            _ => ("pet-rest.png", 8, 4)
        };
        if (!_sheets.TryGetValue(sheet, out var bitmap))
        {
            var path = Path.Combine(_dir, sheet);
            if (!File.Exists(path)) return null;
            var loaded = new BitmapImage();
            loaded.BeginInit();
            loaded.UriSource = new Uri(path, UriKind.Absolute);
            loaded.CacheOption = BitmapCacheOption.OnLoad;
            loaded.EndInit();
            loaded.Freeze();
            _sheets[sheet] = bitmap = loaded;
        }
        var width = bitmap.PixelWidth / count;
        if (width < 1 || frame >= count) return null;
        var x = width * frame;
        var cropWidth = frame == count - 1 ? bitmap.PixelWidth - x : width;
        var crop = new CroppedBitmap(bitmap, new Int32Rect(x, 0, cropWidth, bitmap.PixelHeight));
        crop.Freeze();
        return crop;
    }
}
