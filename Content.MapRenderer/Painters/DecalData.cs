using Content.Shared.Decals;

namespace Content.MapRenderer.Painters;

public readonly record struct DecalData(DecalIndex Index, Decal Decal, float X, float Y);
