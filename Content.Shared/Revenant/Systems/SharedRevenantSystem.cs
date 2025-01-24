using Content.Shared.Examine;
using Content.Shared.Revenant.Components;
using Content.Shared.StatusEffect;

namespace Content.Shared.Revenant.Systems;

public abstract class SharedRevenantSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RevenantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<RevenantComponent, StatusEffectEndedEvent>(OnStatusEnded);
    }

    private void OnExamine(Entity<RevenantComponent> ent, ref ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            args.PushMarkup(Loc.GetString("revenant-essence-amount",
                ("current", ent.Comp.Essence.Int()),
                ("max", ent.Comp.EssenceRegenCap.Int())));
        }
    }

    private void OnStatusAdded(Entity<RevenantComponent> ent, ref StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            Appearance.SetData(ent, RevenantVisuals.Stunned, true);
    }

    private void OnStatusEnded(Entity<RevenantComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
            Appearance.SetData(ent, RevenantVisuals.Stunned, false);
    }
}
