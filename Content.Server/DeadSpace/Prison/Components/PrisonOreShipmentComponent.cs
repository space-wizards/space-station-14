using Content.Shared.Stacks;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Prison.Components;

[RegisterComponent, Access(typeof(PrisonOreSystem))]
public sealed partial class PrisonOreShipmentComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<ProtoId<StackPrototype>, int> Ores = new();

    [ViewVariables]
    public readonly List<PrisonOreContribution> Contributions = new();

    [ViewVariables]
    public bool InTransit;

    [ViewVariables]
    public bool Delivered;

}

public sealed class PrisonOreContribution
{
    public NetUserId UserId;
    public int BanId;
    public long ReductionTicks;
    public bool Processing;

    public PrisonOreContribution(NetUserId userId, int banId, long reductionTicks)
    {
        UserId = userId;
        BanId = banId;
        ReductionTicks = reductionTicks;
    }
}
