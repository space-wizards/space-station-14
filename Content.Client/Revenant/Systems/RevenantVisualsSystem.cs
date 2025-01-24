using Content.Client.Revenant.Components;
using Content.Shared.Revenant;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant.Systems;

public sealed class RevenantVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<RevenantVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(ent, RevenantVisuals.Harvesting, out var harvesting, args.Component) &&
            harvesting)
            args.Sprite.LayerSetState(0, ent.Comp.HarvestingState);
        else if (_appearance.TryGetData<bool>(ent, RevenantVisuals.Stunned, out var stunned, args.Component) && stunned)
            args.Sprite.LayerSetState(0, ent.Comp.StunnedState);
        else if (_appearance.TryGetData<bool>(ent, RevenantVisuals.Corporeal, out var corporeal, args.Component))
            args.Sprite.LayerSetState(0, corporeal ? ent.Comp.CorporealState : ent.Comp.State);
    }
}
