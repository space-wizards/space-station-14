using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Content.Shared.Inventory.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Clothing.Components;
using Content.Shared._Starlight.NullSpace;

namespace Content.Server._Starlight.NullSpace;

public sealed class ShowEtherealSystem : EntitySystem
{
    [Dependency] private readonly EyeSystem _eye = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShowNullSpaceComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ShowNullSpaceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShowNullSpaceComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ShowNullSpaceComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ShowNullSpaceComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ShowNullSpaceComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnInit(EntityUid uid, ShowNullSpaceComponent component, MapInitEvent args)
    {
        Toggle(uid, true);
    }

    public void OnShutdown(EntityUid uid, ShowNullSpaceComponent component, ComponentShutdown args)
    {
        Toggle(uid, false);
    }

    private void OnEquipped(EntityUid uid, ShowNullSpaceComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)
            || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        EnsureComp<ShowNullSpaceComponent>(args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, ShowNullSpaceComponent component, GotUnequippedEvent args)
    {
        RemComp<ShowNullSpaceComponent>(args.Equipee);
    }

    private void Toggle(EntityUid uid, bool toggle)
    {
        if (!TryComp<EyeComponent>(uid, out var eye))
            return;

        if (toggle)
        {
            _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) (VisibilityFlags.NullSpace), eye);
            return;
        }
        else if (HasComp<NullSpaceComponent>(uid))
            return;

        _eye.SetVisibilityMask(uid, (int) VisibilityFlags.Normal, eye);
    }

    private void OnInteractionAttempt(EntityUid uid, ShowNullSpaceComponent component, ref InteractionAttemptEvent args)
    {
        if (!HasComp<NullSpaceComponent>(args.Target))
            return;

        args.Cancelled = true;
    }

    private void OnAttackAttempt(EntityUid uid, ShowNullSpaceComponent component, AttackAttemptEvent args)
    {
        if (HasComp<NullSpaceComponent>(uid)
            || !HasComp<NullSpaceComponent>(args.Target))
            return;

        args.Cancel();
    }
}