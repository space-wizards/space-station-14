using Robust.Shared.Serialization;

namespace Content.Shared.Signature;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SignatureData
{
    [DataField]
    public int Width;

    [DataField]
    public int Height;

    [DataField]
    public byte[] Pixels;

    public SignatureData(int w, int h)
    {
        Width = w;
        Height = h;
        Pixels = new byte[w * h];
    }

    public bool GetPixel(int x, int y)
    {
        return Pixels[y * Width + x] == 1;
    }

    public void SetPixel(int x, int y)
    {
        Pixels[y * Width + x] = 1;
    }

    public void ErasePixel(int x, int y)
    {
        Pixels[y * Width + x] = 0;
    }

    public void Clear()
    {
        Array.Fill(Pixels, (byte)0);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SignatureData other)
            return false;

        if (Width != other.Width || Height != other.Height)
            return false;

        return Pixels.SequenceEqual(other.Pixels);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Width);
        hash.Add(Height);

        hash.AddBytes(Pixels);

        return hash.ToHashCode();
    }

    public SignatureData Clone()
    {
        var copy = new SignatureData(Width, Height);
        Array.Copy(Pixels, copy.Pixels, Pixels.Length);
        return copy;
    }

    public void CopyTo(SignatureData clone)
    {
        var minWidth  = Math.Min(Width,  clone.Width);
        var minHeight = Math.Min(Height, clone.Height);

        for (var y = 0; y < minHeight; y++)
        {
            var originalOffset = y * Width;
            var cloneOffset = y * clone.Width;

            Array.Copy(
                Pixels,
                originalOffset,
                clone.Pixels,
                cloneOffset,
                minWidth
            );
        }
    }
}
