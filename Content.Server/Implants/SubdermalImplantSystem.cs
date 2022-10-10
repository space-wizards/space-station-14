using System.Linq;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.MobState;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(OnMobstateChanged);
    }

    private void OnMobstateChanged(EntityUid uid, ImplantedComponent component, MobStateChangedEvent args)
    {
        if (!_container.TryGetContainer(uid, ImplantSlotId, out var implantContainer))
            return;

        var implant = implantContainer.ContainedEntities.FirstOrDefault(HasComp<TriggerOnMobstateChangeComponent>);

        if (TryComp<TriggerOnMobstateChangeComponent>(implant, out var trigger) && trigger.MobState == args.CurrentMobState)
        {
            _trigger.Trigger(implant);

            if (TryComp<SharedBodyComponent>(uid, out var body) && trigger.GibOnDeath)
                body.Gib();
        }
    }
}
