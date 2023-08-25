using Content.Server.DeviceLinking.Systems;
using Content.Shared.Slippery;
using Robust.Shared.Prototypes;

namespace Content.Server.Slippery;

public sealed partial class SlipSignalSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipSignalComponent, SlipEvent>(SendSlipSignal);
    }

    private void SendSlipSignal(EntityUid uid, SlipSignalComponent comp, ref SlipEvent args)
    {
        _deviceLinkSystem.InvokePort(uid, comp.Port);
    }
}
