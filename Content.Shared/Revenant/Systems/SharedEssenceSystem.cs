using Content.Shared.Examine;
using Content.Shared.Revenant.Components;

namespace Content.Shared.Revenant.Systems;

public abstract class SharedEssenceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EssenceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<EssenceComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.SearchComplete || !HasComp<RevenantComponent>(args.Examiner))
            return;

        var message = ent.Comp.EssenceAmount switch
        {
            <= 45 => "revenant-soul-yield-low",
            >= 90 => "revenant-soul-yield-high",
            _ => "revenant-soul-yield-average",
        };

        args.PushMarkup(Loc.GetString(message, ("target", ent)));
    }
}
