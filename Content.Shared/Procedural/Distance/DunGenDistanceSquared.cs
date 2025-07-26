namespace Content.Shared.Procedural.Distance;

public sealed partial class DunGenDistanceSquared : IDunGenDistance
{
    [DataField]
    public float BlendWeight { get; set; } = 0.50f;
}
