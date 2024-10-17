using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ContainmentAlarmHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public ItemSlot AlarmSlot = new();
}
