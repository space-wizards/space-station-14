using Content.Shared.Decals;

namespace Content.MapRenderer.Painters;

public sealed class DecalData
{
    public DecalData(Decal decal, float x, float y)
    {
        Decal = decal;
        X = x;
        Y = y;
    }

    public Decal Decal;

    public float X;
    public float Y;
}
