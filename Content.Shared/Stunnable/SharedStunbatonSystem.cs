using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Power.Components;
using Content.Shared.Stunnable.Components;

namespace Content.Shared.Stunnable;

public abstract class SharedStunbatonSystem : EntitySystem
{
    [Dependency] protected readonly SharedItemToggleSystem ItemToggle = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<StunbatonComponent> entity, ref ExaminedEvent args)
    {
        var onMsg = ItemToggle.IsActivated(entity.Owner)
        ? Loc.GetString("comp-stunbaton-examined-on")
        : Loc.GetString("comp-stunbaton-examined-off");
        args.PushMarkup(onMsg);

        if (TryComp<BatteryComponent>(entity.Owner, out var battery))
        {
            var count = (int) (battery.CurrentCharge / entity.Comp.EnergyPerUse);
            args.PushMarkup(Loc.GetString("melee-battery-examine", ("color", "yellow"), ("count", count)));
        }
    }
}
