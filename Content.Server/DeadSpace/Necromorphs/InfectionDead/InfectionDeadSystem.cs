// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Mobs.Components;
using Content.Shared.Humanoid;

namespace Content.Server.DeadSpace.Necromorphs.InfectionDead;

public sealed class InfectionDeadSystem : SharedInfectionDeadSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly NecromorfSystem _necromorfSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectionDeadComponent, InfectionDeadDamageEvent>(GetDamage);
        SubscribeLocalEvent<InfectionDeadComponent, InfectionNecroficationEvent>(OnState);
    }

    private void GetDamage(EntityUid uid, InfectionDeadComponent component, ref InfectionDeadDamageEvent args)
    {
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cellular", 15f);
        _damage.TryChangeDamage(uid, dspec, true, false);
    }

    private void OnState(EntityUid uid, InfectionDeadComponent component, InfectionNecroficationEvent args)
    {
        if (!_mobState.IsDead(uid))
            return;

        if (!TryComp<MobStateComponent>(uid, out var mobStateComponent))
            return;

        var isAnimal = false;

        if (HasComp<HumanoidAppearanceComponent>(uid))
            isAnimal = false;
        else
            isAnimal = true;

        var necromorf = GetRandomNecromorfPrototypeId(isAnimal);

        if (EntityManager.TryGetComponent<NecromorfAfterInfectionComponent>(uid, out var necroComponent))
            necromorf = necroComponent.NecroPrototype;

        if (necromorf != null)
            _necromorfSystem.Necrofication(uid, necromorf, mobStateComponent);

        RemComp<InfectionDeadComponent>(uid);
    }
}
