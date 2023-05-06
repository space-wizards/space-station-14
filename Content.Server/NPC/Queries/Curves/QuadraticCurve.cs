namespace Content.Server.NPC.Queries.Curves;

public sealed class QuadraticCurve : IUtilityCurve
{
    [DataField("slope")] public readonly float Slope;

    [DataField("exponent")] public readonly float Exponent;

    [DataField("yOffset")] public readonly float YOffset;

    [DataField("xOffset")] public readonly float XOffset;
}
