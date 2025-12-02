using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnLandComponent : Component
{
    /// <summary>
    /// Should this entity be damaged when it lands regardless of its resistances?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances = false;

    /// <summary>
    /// How much damage.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();
}
