using Content.Server.DeviceLinking.Systems;
using Content.Server.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;

namespace Content.Server.StepTrigger.Systems;

public sealed partial class StepTriggerSignalSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StepTriggerSignalComponent, StepTriggeredEvent>(SendSignal);
    }

    private void SendSignal(EntityUid uid, StepTriggerSignalComponent comp, ref StepTriggeredEvent args)
    {
        _deviceLinkSystem.InvokePort(uid, comp.Port);
    }
}
