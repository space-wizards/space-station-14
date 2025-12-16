using Content.Shared.DeepFryer.Components;
using Content.Shared.Examine;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared.DeepFryer;

public abstract class SharedBeenFriedSystem : EntitySystem
{
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeenFriedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BeenFriedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BeenFriedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<BeenFriedComponent, FlavorProfileModificationEvent>(OnFlavorProfileModifiers);
    }

    private void OnInit(EntityUid uid, BeenFriedComponent component, ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(uid);
    }

    private void OnExamine(Entity<BeenFriedComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BeenFriedComponent)))
        {
            args.PushMarkup(Loc.GetString("fried-on-examine-details"));
        }
    }

    private void OnRefreshNameModifiers(Entity<BeenFriedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("fried-name-prefix");
    }

    private void OnFlavorProfileModifiers(Entity<BeenFriedComponent> ent, ref FlavorProfileModificationEvent args)
    {
        args.Flavors.Add("fried");
    }
}
