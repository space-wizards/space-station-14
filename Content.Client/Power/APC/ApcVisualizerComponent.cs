namespace Content.Client.Power.APC;

[RegisterComponent]
[Access(typeof(ApcVisualizerSystem))]
public sealed class ApcVisualizerComponent : Component
{
    public static readonly Color LackColor = Color.FromHex("#d1332e");
    public static readonly Color ChargingColor = Color.FromHex("#2e8ad1");
    public static readonly Color FullColor = Color.FromHex("#3db83b");
    public static readonly Color EmagColor = Color.FromHex("#1f48d6");
}
