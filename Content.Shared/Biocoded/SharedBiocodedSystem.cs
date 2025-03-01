using Content.Shared.Forensics.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;

namespace Content.Shared.Biocoded;

/// <summary>
/// System used for canceling interaction events if fingerprints don't match those provided.
/// </summary>
public sealed class SharedBiocodedSystem : EntitySystem
{

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiocodedComponent, GettingUsedAttemptEvent>(OnGettingUsed);
    }

    private void OnGettingUsed(Entity<BiocodedComponent> ent, ref GettingUsedAttemptEvent args)
    {
        if (ent.Comp.Fingerprint == null)
            return;

        if (_inventory.TryGetSlotEntity(args.User, "gloves", out var gloves) && HasComp<FingerprintMaskComponent>(gloves) && !ent.Comp.IgnoreGloves)
        {
            if (ent.Comp.FailGlovesPopup != null)
                _popup.PopupClient(Loc.GetString(ent.Comp.FailGlovesPopup), args.User, args.User);
            args.Cancel();
            return;
        }

        if (!TryComp<FingerprintComponent>(args.User, out var fingerprint) ||
            ent.Comp.Fingerprint != fingerprint.Fingerprint)
        {
            if (ent.Comp.FailPopup != null)
                _popup.PopupClient(Loc.GetString(ent.Comp.FailPopup), args.User, args.User);
            args.Cancel();
            return;
        }
    }
}
