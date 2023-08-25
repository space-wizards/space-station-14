using Content.Server.DeviceLinking.Systems;
using Content.Server.Damage.Components;
using Content.Shared.Damage;

namespace Content.Server.Damage.Systems;

public sealed partial class DamageSignalSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLinkSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageSignalComponent, DamageChangedEvent>(SendSignal);
    }

    private void SendSignal(EntityUid uid, DamageSignalComponent comp, DamageChangedEvent args)
    {
        if (!args.DamageIncreased // The damage dealt shouldn't be healing
        || args.Origin == null // The damage dealt should also probably be done by an actual entity
        || args.Origin == uid) // And the damage shouldn't be caused by the entity itself.
            return;

        _deviceLinkSystem.InvokePort(uid, comp.Port);
    }
}
