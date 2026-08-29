using Content.Shared.Examine;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// This system handles showing how many more times a weapon can hit things before the battery is depleted.
/// </summary>
public sealed partial class MeleeBatteryHitsLeftSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;

    [Dependency] private EntityQuery<MeleeBatteryHitsLeftComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<MeleeBatteryHitsLeftComponent> ent, ref MapInitEvent args)
    {
        UpdateHitPowerCost(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<MeleeBatteryHitsLeftComponent> ent, ref ExaminedEvent args)
    {
        var count = _battery.GetRemainingUses(ent.Owner, ent.Comp.HitPowerCost);
        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText, ("color", ent.Comp.Color), ("count", count)));
    }

    /// <summary>
    /// Updates how much battery power it costs to hit with this weapon.
    /// </summary>
    /// <param name="ent">The entity to update for.</param>
    [PublicAPI]
    public void UpdateHitPowerCost(Entity<MeleeBatteryHitsLeftComponent?> ent)
    {
        if (!_query.Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new ModifyHitPowerCostEvent();
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.HitPowerCost = ev.Cost;
        DirtyField(ent, nameof(MeleeBatteryHitsLeftComponent.HitPowerCost));
    }
}
