using Robust.Shared.Serialization;

namespace Content.Shared.Instruments.UI;

[Serializable, NetSerializable]
public sealed partial class InstrumentBandRequestBuiMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed partial class InstrumentBandResponseBuiMessage : BoundUserInterfaceMessage
{
    public (NetEntity, string)[] Nearby { get; set; }

    public InstrumentBandResponseBuiMessage((NetEntity, string)[] nearby)
    {
        Nearby = nearby;
    }
}

