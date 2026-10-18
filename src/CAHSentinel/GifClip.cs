using System.Buffers.Binary;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CAHSentinel;

/// <summary>Bounded local GIF decode with explicit transparent-frame disposal/composition.</summary>
public sealed record GifClip(IReadOnlyList<BitmapSource> Frames, IReadOnlyList<TimeSpan> Delays)
{
    public static GifClip Load(string path)
    {
        var info = new FileInfo(path);
        if (info.Length is < 6 or > 16 * 1024 * 1024) throw new InvalidDataException("GIF file size limit.");
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[13];
        stream.ReadExactly(header);
        if (!header[..6].SequenceEqual("GIF89a"u8) && !header[..6].SequenceEqual("GIF87a"u8))
            throw new InvalidDataException("Not a GIF.");
        var width = (int)BinaryPrimitives.ReadUInt16LittleEndian(header[6..8]);
        var height = (int)BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]);
        if (width is < 1 or > 1024 || height is < 1 or > 1024)
            throw new InvalidDataException("GIF canvas limit.");
        byte[] background = [0, 0, 0, 0];
        if ((header[10] & 0x80) != 0)
        {
            var palette = new byte[3 * (2 << (header[10] & 7))];
            stream.ReadExactly(palette);
            var index = header[11] * 3;
            if (index + 2 < palette.Length)
                background = [palette[index + 2], palette[index + 1], palette[index], 255];
        }
        // Inspect frame count/rectangles before native WIC decoding allocates surfaces.
        // Compressed inputs can be tiny despite a very large decompressed footprint.
        var count = ValidateStructure(stream, width, height);
        stream.Position = 0;
        var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count != count) throw new InvalidDataException("GIF frame count mismatch.");
        var first = decoder.Frames[0];
        var stride = checked(width * 4);
        var canvas = new byte[checked(stride * height)];
        if (Number(first.Metadata, "/grctlext/TransparencyFlag", 0) == 0)
            for (var i = 0; i < canvas.Length; i += 4) background.CopyTo(canvas, i);
        var frames = new List<BitmapSource>();
        var delays = new List<TimeSpan>();
        foreach (var frame in decoder.Frames)
        {
            var left = Number(frame.Metadata, "/imgdesc/Left", 0);
            var top = Number(frame.Metadata, "/imgdesc/Top", 0);
            var w = frame.PixelWidth;
            var h = frame.PixelHeight;
            if (left < 0 || top < 0 || w < 1 || h < 1 || left + w > width || top + h > height)
                throw new InvalidDataException("GIF frame escapes canvas.");
            var disposal = Number(frame.Metadata, "/grctlext/Disposal", 0);
            if (disposal is < 0 or > 3) throw new InvalidDataException("Unsupported GIF disposal.");
            var restore = disposal == 3 ? (byte[])canvas.Clone() : null;
            var pixels = new byte[checked(w * h * 4)];
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Pbgra32, null, 0);
            converted.CopyPixels(pixels, w * 4, 0);
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var src = (y * w + x) * 4;
                var dst = (top + y) * stride + (left + x) * 4;
                var alpha = pixels[src + 3];
                // GIF transparency is binary; preserve earlier pixels under transparent patches.
                if (alpha == 0) continue;
                for (var c = 0; c < 4; c++) canvas[dst + c] = pixels[src + c];
            }
            var composed = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32,
                null, canvas, stride);
            composed.Freeze();
            frames.Add(composed);
            // GIF centiseconds. Zero/absent delays use a safe 100ms, positive delays are exact.
            var delay = Number(frame.Metadata, "/grctlext/Delay", 0);
            delays.Add(TimeSpan.FromMilliseconds(delay > 0 ? delay * 10 : 100));
            if (disposal == 2)
            {
                var clear = Number(frame.Metadata, "/grctlext/TransparencyFlag", 0) != 0;
                for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var offset = (top + y) * stride + (left + x) * 4;
                    if (clear) Array.Clear(canvas, offset, 4);
                    else background.CopyTo(canvas, offset);
                }
            }
            else if (restore is not null) canvas = restore;
        }
        return new(frames, delays);
    }

    private static int Number(object? metadata, string query, int fallback)
    {
        if (metadata is not BitmapMetadata bitmap || !bitmap.ContainsQuery(query)) return fallback;
        return Convert.ToInt32(bitmap.GetQuery(query));
    }

    private static int ValidateStructure(Stream stream, int width, int height)
    {
        // Positioned just after header/global color table. Extensions and LZW data use
        // bounded sub-blocks; no decoding, scripts, URLs or arbitrary metadata execution.
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        void Skip(int bytes)
        {
            if (stream.Length - stream.Position < bytes) throw new EndOfStreamException();
            stream.Position += bytes;
        }
        void SkipBlocks()
        {
            for (var size = reader.ReadByte(); size != 0; size = reader.ReadByte()) Skip(size);
        }
        var count = 0;
        while (true)
        {
            switch (reader.ReadByte())
            {
                case 0x3b:
                    if (count == 0) throw new InvalidDataException("Empty GIF.");
                    return count;
                case 0x21:
                    reader.ReadByte(); // extension label
                    SkipBlocks();
                    break;
                case 0x2c:
                    var left = reader.ReadUInt16();
                    var top = reader.ReadUInt16();
                    var w = reader.ReadUInt16();
                    var h = reader.ReadUInt16();
                    var packed = reader.ReadByte();
                    if (w == 0 || h == 0 || left + w > width || top + h > height)
                        throw new InvalidDataException("GIF frame escapes canvas.");
                    if (++count > 256 || (long)width * height * count > 32_000_000)
                        throw new InvalidDataException("GIF decoded pixel/frame limit.");
                    if ((packed & 0x80) != 0) Skip(3 * (2 << (packed & 7)));
                    var codeSize = reader.ReadByte();
                    if (codeSize is < 2 or > 8) throw new InvalidDataException("Invalid GIF LZW code size.");
                    SkipBlocks();
                    break;
                default:
                    throw new InvalidDataException("Invalid GIF block.");
            }
        }
    }
}
