namespace Content.Shared._Offbrand.Maths;

[ImplicitDataDefinitionForInheritors]
public partial interface ICurve
{
    // TODO: genericize with generic math
    float Value(float v);
}

public static class ICurveExtensions
{
    public static float Clamped(this ICurve curve, float v)
    {
        return Math.Clamp(curve.Value(v), 0f, 1f);
    }
}
