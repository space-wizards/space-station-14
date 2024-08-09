using Content.Shared.Emag.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared.Cabinet;

public sealed class SealingCabinetSystem : EntitySystem
{
    [Dependency] private readonly ItemCabinetSystem _cabinet = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SealingCabinetComponent, OpenableOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<SealingCabinetComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnOpenAttempt(Entity<SealingCabinetComponent> ent, ref OpenableOpenAttemptEvent args)
    {
        if (!_cabinet.HasItem(ent.Owner))
            return;

        args.Cancelled = true;
        if (args.User is {} user)
            _popup.PopupClient(Loc.GetString(ent.Comp.SealedPopup, ("container", ent.Owner)), ent, user);
    }

    private void OnEmagged(Entity<SealingCabinetComponent> ent, ref GotEmaggedEvent args)
    {
        if (!ent.Comp.Emaggable)
            return;

        if (!_cabinet.HasItem(ent.Owner) || _openable.IsOpen(ent))
            return;

        _openable.SetOpen(ent, true);

        args.Handled = true;
        args.Repeatable = true;
    }
}
