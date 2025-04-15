using Content.Shared.Eye;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

public abstract class SharedTrayScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public const float SubfloorRevealAlpha = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ComponentGetState>(OnTrayScannerGetState);
        SubscribeLocalEvent<TrayScannerComponent, ComponentHandleState>(OnTrayScannerHandleState);
        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);

        SubscribeLocalEvent<TrayScannerComponent, GotEquippedHandEvent>(OnTrayHandEquipped);
        SubscribeLocalEvent<TrayScannerComponent, GotUnequippedHandEvent>(OnTrayHandUnequipped);
        SubscribeLocalEvent<TrayScannerComponent, GotEquippedEvent>(OnTrayEquipped);
        SubscribeLocalEvent<TrayScannerComponent, GotUnequippedEvent>(OnTrayUnequipped);

        SubscribeLocalEvent<TrayScannerUserComponent, GetVisMaskEvent>(OnUserGetVis);
    }

    private void OnUserGetVis(Entity<TrayScannerUserComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    private void OnEquip(EntityUid user)
    {
        EnsureComp<TrayScannerUserComponent>(user);
        _eye.RefreshVisibilityMask(user);
    }

    private void OnUnequip(EntityUid user)
    {
        RemComp<TrayScannerUserComponent>(user);
        _eye.RefreshVisibilityMask(user);
    }

    private void OnTrayHandUnequipped(Entity<TrayScannerComponent> ent, ref GotUnequippedHandEvent args)
    {
        OnUnequip(args.User);
    }

    private void OnTrayHandEquipped(Entity<TrayScannerComponent> ent, ref GotEquippedHandEvent args)
    {
        OnEquip(args.User);
    }

    private void OnTrayUnequipped(Entity<TrayScannerComponent> ent, ref GotUnequippedEvent args)
    {
        OnUnequip(args.Equipee);
    }

    private void OnTrayEquipped(Entity<TrayScannerComponent> ent, ref GotEquippedEvent args)
    {
        OnEquip(args.Equipee);
    }

    private void OnTrayScannerActivate(EntityUid uid, TrayScannerComponent scanner, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        SetScannerEnabled(uid, !scanner.Enabled, scanner);
        args.Handled = true;
    }

    private void SetScannerEnabled(EntityUid uid, bool enabled, TrayScannerComponent? scanner = null)
    {
        if (!Resolve(uid, ref scanner) || scanner.Enabled == enabled)
            return;

        scanner.Enabled = enabled;
        Dirty(uid, scanner);

        // We don't remove from _activeScanners on disabled, because the update function will handle that, as well as
        // managing the revealed subfloor entities

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, TrayScannerVisual.Visual, scanner.Enabled ? TrayScannerVisual.On : TrayScannerVisual.Off, appearance);
        }
    }

    private void OnTrayScannerGetState(EntityUid uid, TrayScannerComponent scanner, ref ComponentGetState args)
    {
        args.State = new TrayScannerState(scanner.Enabled, scanner.Range);
    }

    private void OnTrayScannerHandleState(EntityUid uid, TrayScannerComponent scanner, ref ComponentHandleState args)
    {
        if (args.Current is not TrayScannerState state)
            return;

        scanner.Range = state.Range;
        SetScannerEnabled(uid, state.Enabled, scanner);
    }
}

[Serializable, NetSerializable]
public enum TrayScannerVisual : sbyte
{
    Visual,
    On,
    Off
}
