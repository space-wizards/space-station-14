using Robust.Shared.Serialization;

namespace Content.Shared.Instruments.UI;

[Serializable, NetSerializable]
public sealed class InstrumentBandRequestBuiMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class InstrumentBandResponseBuiMessage : BoundUserInterfaceMessage
{
    public (EntityUid, string)[] Nearby { get; set; }

    public InstrumentBandResponseBuiMessage((EntityUid, string)[] nearby)
    {
        Nearby = nearby;
    }
}
