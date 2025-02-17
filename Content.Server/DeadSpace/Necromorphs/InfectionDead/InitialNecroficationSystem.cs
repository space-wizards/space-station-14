// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Humanoid;

namespace Content.Server.DeadSpace.Necromorphs.InfectionDead;

public sealed partial class InitialNecroficationSystem : SharedInfectionDeadSystem
{
    [Dependency] private readonly NecromorfSystem _necromorfSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InitialNecroficationComponent, ComponentStartup>(OnComponentStartUp);
        SubscribeLocalEvent<InitialNecroficationComponent, StartNecroficationEvent>(StartNecrofication);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var necroQuery = EntityQueryEnumerator<InitialNecroficationComponent>();
        while (necroQuery.MoveNext(out var uid, out var component))
        {
            // Process only once per second
            if (component.StartTick > curTime)
            {
                continue;
            }
            else
            {
                var startNecroficationEvent = new StartNecroficationEvent();
                RaiseLocalEvent(uid, ref startNecroficationEvent);
                continue;
            }
        }
    }

    private void OnComponentStartUp(EntityUid uid, InitialNecroficationComponent component, ComponentStartup arg)
    {
        if (HasComp<NecromorfComponent>(uid))
            return;

        component.StartTick = _timing.CurTime + TimeSpan.FromSeconds(0.2);
    }

    private void StartNecrofication(EntityUid uid, InitialNecroficationComponent component, StartNecroficationEvent arg)
    {
        if (HasComp<NecromorfComponent>(uid))
            return;

        if (!TryComp<MobStateComponent>(uid, out var mobStateComponent))
            return;

        if (!string.IsNullOrEmpty(component.NecroPrototype) && component.NecroPrototype != "Random")
        {
            _necromorfSystem.Necrofication(uid, component.NecroPrototype, mobStateComponent);
        }
        else
        {
            var isAnimal = false;

            if (HasComp<HumanoidAppearanceComponent>(uid))
                isAnimal = false;
            else
                isAnimal = true;

            var necromorf = GetRandomNecromorfPrototypeId(isAnimal);

            _necromorfSystem.Necrofication(uid, necromorf, mobStateComponent);
        }

        RemComp<InitialNecroficationComponent>(uid);
        return;
    }
}
