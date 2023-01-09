namespace Content.Client.DamageState;

[RegisterComponent]
public sealed class DamageStateVisualsComponent : Component
{
    public int? OriginalDrawDepth;

    [DataField("states")] public Dictionary<Shared.Mobs.MobState, Dictionary<DamageStateVisualLayers, string>> States = new();

    /// <summary>
    /// Should noRot be turned off when crit / dead.
    /// </summary>
    [DataField("rotate")] public bool Rotate;
}

public enum DamageStateVisualLayers : byte
{
    Base,
    BaseUnshaded,
}
