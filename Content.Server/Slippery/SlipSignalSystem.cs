using Content.Server.DeviceLinking.Systems;
using Content.Shared.Slippery;

namespace Content.Server.Slippery;

public sealed partial class SlipSignalSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipSignalComponent, SlipEvent>(SendSignal);
    }

    private void SendSignal(EntityUid uid, SlipSignalComponent comp, ref SlipEvent args)
    {
        _deviceLinkSystem.InvokePort(uid, comp.Port);
    }
}
