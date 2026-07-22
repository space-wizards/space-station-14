// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Weapons;

public sealed class ExclusiveHandUseSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<HandsComponent, AttackAttemptEvent>(OnAttackAttempted);
    }

    private void OnShotAttempted(Entity<HandsComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!TryGetBlocker(ent, args.Used, out var blocker))
            return;

        args.Cancel();
        ShowPopup(ent, blocker);
    }

    private void OnAttackAttempted(Entity<HandsComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Weapon is not { } weapon ||
            !TryGetBlocker(ent, weapon.Owner, out var blocker))
            return;

        args.Cancel();
        ShowPopup(ent, blocker);
    }

    private bool TryGetBlocker(
        Entity<HandsComponent> holder,
        EntityUid used,
        out ExclusiveHandUseComponent blocker)
    {
        var usedPrototype = MetaData(used).EntityPrototype?.ID;
        if (usedPrototype == null)
        {
            blocker = default!;
            return false;
        }

        // The restriction can be placed directly on the user.
        if (TryComp<ExclusiveHandUseComponent>(holder, out var holderComponent) &&
            IsBlocked(holderComponent, usedPrototype))
        {
            blocker = holderComponent;
            return true;
        }

        // It can also be supplied by another held item, such as a shield.
        foreach (var held in _hands.EnumerateHeld((holder.Owner, holder.Comp)))
        {
            if (held == used || !TryComp<ExclusiveHandUseComponent>(held, out var component))
                continue;

            if (!IsBlocked(component, usedPrototype))
                continue;

            blocker = component;
            return true;
        }

        blocker = default!;
        return false;
    }

    private static bool IsBlocked(ExclusiveHandUseComponent component, EntProtoId usedPrototype)
    {
        if (component.BlockedItems.Contains(usedPrototype))
            return true;

        return component.AllowedItems.Count > 0 && !component.AllowedItems.Contains(usedPrototype);
    }

    private void ShowPopup(EntityUid holder, ExclusiveHandUseComponent component)
    {
        _popup.PopupClient(Loc.GetString(component.Popup), holder, holder, PopupType.SmallCaution);
    }
}
