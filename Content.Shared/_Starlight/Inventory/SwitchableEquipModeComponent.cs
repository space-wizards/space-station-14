using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Inventory;

public enum EquipMode
{
    Remove,
    Open
};

/// <summary>
/// Gives user-equipped storage a toggle so that the default action upon clicking the equipped bag can be switched from unequip to open
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SwitchableEquipModeComponent : Component
{
    [AutoNetworkedField]
    public EquipMode Mode = EquipMode.Remove;
}