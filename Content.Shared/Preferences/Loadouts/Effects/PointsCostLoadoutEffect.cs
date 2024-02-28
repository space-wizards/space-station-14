namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Specifies the loadout as requiring points.
/// </summary>
public sealed partial class PointsCostLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public int Cost;
}
