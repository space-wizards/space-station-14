using Content.Shared.Changeling.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Entities with this component resist being destructed (E.g gibbed, exploded etc...)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DestructionResistanceSystem))]
public sealed partial class DestructionResistanceComponent : Component
{
    /// <summary>
    /// If false, the component deactivates and the entity can now be destroyed again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
