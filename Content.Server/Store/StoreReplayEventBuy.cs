using Content.Server.GameTicking.Replays;

namespace Content.Server.Store.Components;

[Serializable, DataDefinition]
public sealed partial class StoreReplayEventBuy : ReplayEvent
{
    [DataField]
    public ReplayEventPlayer Buyer;

    [DataField]
    public string Item = string.Empty;

    [DataField]
    public int Cost;
}
