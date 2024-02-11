using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs;

[Serializable, NetSerializable]
public enum BorgUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class BorgBuiState : BoundUserInterfaceState
{
    public float ChargePercent;

    public bool HasBattery;

    public BorgBuiState(float chargePercent, bool hasBattery)
    {
        ChargePercent = chargePercent;
        HasBattery = hasBattery;
    }
}

[Serializable, NetSerializable]
public sealed class BorgEjectBrainBuiMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class BorgEjectBatteryBuiMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class BorgSetNameBuiMessage : BoundUserInterfaceMessage
{
    public string Name;

    public BorgSetNameBuiMessage(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class BorgRemoveModuleBuiMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public BorgRemoveModuleBuiMessage(NetEntity module)
    {
        Module = module;
    }
}
