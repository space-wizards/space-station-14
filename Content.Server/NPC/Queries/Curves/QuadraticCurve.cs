namespace Content.Server.NPC.Queries.Curves;

public sealed class QuadraticCurve : IUtilityCurve
{
    [DataField("slope")] public float Slope { get; private set; } = 1f;

    [DataField("exponent")] public float Exponent { get; private set; } = 1f;

    [DataField("yOffset")] public float YOffset { get; private set; }

    [DataField("xOffset")] public float XOffset { get; private set; }
}
