using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Weapons.Melee;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    protected virtual void InitializeSpillable()
    {
        SubscribeLocalEvent<SpillableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<SpillableComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(SpillableComponent)))
        {
            args.PushMarkup(Loc.GetString("spill-examine-is-spillable"));

            if (HasComp<MeleeWeaponComponent>(entity))
                args.PushMarkup(Loc.GetString("spill-examine-spillable-weapon"));
        }
    }
}
