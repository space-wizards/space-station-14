using Content.Shared.Containers.ItemSlots;

namespace Content.Server._FTL.Economy;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class IdAtmComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)] public readonly string MoneyContainerId = "Money-Slot";
    [ViewVariables(VVAccess.ReadOnly)] public readonly string IdContainerId = "Id-Slot";

    [DataField("moneySlot"), ViewVariables]
    public ItemSlot MoneySlot = new();

    [DataField("idSlot"), ViewVariables]
    public ItemSlot IdSlot = new();
}
