namespace Content.Shared._Offbrand.Maths;

[Serializable]
public sealed partial class LogisticCurve : ICurve
{
    [DataField(required: true)]
    public float L;

    [DataField(required: true)]
    public float K;

    [DataField(required: true)]
    public float X0;

    public float Value(float v)
    {
        return L / (1 + MathF.Exp(-(K * v + X0)));
    }
}
