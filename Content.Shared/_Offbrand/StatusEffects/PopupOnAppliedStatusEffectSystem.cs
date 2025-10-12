using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class PopupOnAppliedStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PopupOnAppliedStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
    }

    private void OnStatusEffectApplied(Entity<PopupOnAppliedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _popup.PopupClient(Loc.GetString(ent.Comp.Message), args.Target, args.Target, ent.Comp.VisualType);
    }
}
