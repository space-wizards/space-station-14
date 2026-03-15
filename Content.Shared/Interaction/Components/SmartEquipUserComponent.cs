using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SmartEquipUserComponent : Component
{
    [DataField]
    public EntityUid? LastOpenedStorage;
}
