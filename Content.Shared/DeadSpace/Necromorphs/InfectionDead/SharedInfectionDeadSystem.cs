// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectorDead;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Zombies;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead;

public abstract class SharedInfectionDeadSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool IsInfectionPossible(EntityUid target)
    {
        if (!HasComp<MobStateComponent>(target) || HasComp<InfectorDeadComponent>(target) || HasComp<ImmunitetInfectionDeadComponent>(target))
        {
            return false;
        }

        if (HasComp<InfectionDeadComponent>(target) || HasComp<NecromorfComponent>(target))
        {
            return false;
        }

        if (HasComp<ZombieComponent>(target) || HasComp<PendingZombieComponent>(target))
            return false;

        return true;
    }

    private void ActivityInfectionDead(EntityUid uid, InfectionDeadComponent component)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryHandleInfectionState(uid, component))
            return;

        component.NextDamageTime = _timing.CurTime + component.DamageDuration;
    }

    public bool TryHandleInfectionState(EntityUid uid, InfectionDeadComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!_mobState.IsDead(uid))
        {
            var damageEvent = new InfectionDeadDamageEvent();
            RaiseLocalEvent(uid, ref damageEvent);
            return true;
        }

        var necroficationEvent = new InfectionNecroficationEvent();
        RaiseLocalEvent(uid, ref necroficationEvent);

        return true;
    }

    public string GetRandomNecromorfPrototypeId(bool isAnimal = false)
    {
        var filteredNecromorfs = _prototypeManager
            .EnumeratePrototypes<NecromorfPrototype>()
            .Where(necromorf => necromorf.IsAnimal == isAnimal && necromorf.IsCanSpawnInfectionDead)
            .ToList();

        if (!filteredNecromorfs.Any())
            throw new InvalidOperationException("Нет доступных некроморфов для заданных условий!");

        var random = new System.Random();
        var randomIndex = random.Next(0, filteredNecromorfs.Count);

        return filteredNecromorfs[randomIndex].ID;
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var infectionDeadQuery = EntityQueryEnumerator<InfectionDeadComponent>();
        while (infectionDeadQuery.MoveNext(out var ent, out var infectionDead))
        {
            if (_timing.CurTime > infectionDead.NextDamageTime)
                ActivityInfectionDead(ent, infectionDead);

        }

        var query = EntityQueryEnumerator<NecromorfLayerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime > comp.TimeUtilUpdate)
            {
                RaiseNetworkEvent(new RequestNecroficationEvent(GetNetEntity(uid), comp.Sprite, comp.State, comp.IsAnimal));
                comp.TimeUtilUpdate = _timing.CurTime + TimeSpan.FromSeconds(1);
            }
        }

    }
}
