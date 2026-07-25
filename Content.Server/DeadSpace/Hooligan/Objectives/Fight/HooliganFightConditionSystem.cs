// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Server.Objectives.Components;
using System;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Hooligan.Objectives;


/// <summary>
/// Логика цели "Стравить": считает урон, нанесённый атакующим жертве,
/// вешает компонент-счётчик на тело жертвы и отвечает на вопрос о прогрессе.
/// </summary>
public sealed class HooliganFightConditionSystem : EntitySystem
{

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HooliganFightConditionComponent, ObjectiveAfterAssignEvent>(OnAssign);
        SubscribeLocalEvent<HooliganFightTargetComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<HooliganFightConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssign(Entity<HooliganFightConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        var humans = _mind.GetAliveHumans(args.MindId);
        _mind.FilterMinds(humans, ent.Comp.Filters, args.MindId);
        if (humans.Count < 2)
            return;

        var victimMind = _random.Pick(humans); // кого будут бить
        humans.Remove(victimMind);
        var attackerMind = _random.Pick(humans); // кто будет бить

        if (victimMind.Comp.OwnedEntity is not {} victim)
            return;
        if (attackerMind.Comp.OwnedEntity is not {} attacker)
            return;

        var tracker = EnsureComp<HooliganFightTargetComponent>(victim);
        tracker.AttackersByObjective[ent.Owner] = attacker;

        var victimName = victimMind.Comp.CharacterName ?? "bruh";
        var attackerName = attackerMind.Comp.CharacterName ?? "bruh";
        var title = Loc.GetString("objective-condition-hooligan-fight-title", ("attackerName", attackerName), ("victimName", victimName));
        _metaData.SetEntityName(ent.Owner, title, args.Meta);
    }

    private void OnDamaged(Entity<HooliganFightTargetComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var damage = args.DamageDelta.GetTotal();
        foreach (var (objective, attacker) in ent.Comp.AttackersByObjective)
        {
            if (args.Origin != attacker)
                continue;
            if (!TryComp<HooliganFightConditionComponent>(objective, out var condition))
                continue;

            condition.DamageDealt += damage;
        }
    }

    private void OnGetProgress(Entity<HooliganFightConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!TryComp<NumberObjectiveComponent>(ent.Owner, out var number) || number.Target == 0)
        {
            args.Progress = 1f;
            return;
        }
        args.Progress = MathF.Min(ent.Comp.DamageDealt.Float() / number.Target, 1f);
    }
}
