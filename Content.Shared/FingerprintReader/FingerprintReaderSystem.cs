using Content.Shared.Forensics.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using JetBrains.Annotations;

namespace Content.Shared.FingerprintReader;

// TOOD: This has a lot of overlap with the AccessReaderSystem, maybe merge them in the future?
public sealed class FingerprintReaderSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Checks if the given user has fingerprint access to the target entity
    /// </summary>
    [PublicAPI]
    public bool IsAllowed(Entity<FingerprintReaderComponent?> target, EntityUid user)
    {
        if (!Resolve(target, ref target.Comp, false))
            return true;

        if (!target.Comp.Enabled)
            return true;

        if (target.Comp.AllowedFingerprints.Count == 0)
            return true;

        // Check for gloves first
        if (HasBlockingGloves(user) && !target.Comp.IgnoreGloves)
        {
            if (target.Comp.FailGlovesPopup != null)
                _popup.PopupEntity(Loc.GetString(target.Comp.FailGlovesPopup), target, user);
            return false;
        }


        // Check fingerprint match
        if (!TryComp<FingerprintComponent>(user, out var fingerprint) || fingerprint.Fingerprint == null ||
            !target.Comp.AllowedFingerprints.Contains(fingerprint.Fingerprint))
        {
            if (target.Comp.FailPopup != null)
                _popup.PopupEntity(Loc.GetString(target.Comp.FailPopup), target, user);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks whether the user has gloves that block fingerprints
    /// </summary>
    public bool HasBlockingGloves(EntityUid user)
    {
        if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves) && HasComp<FingerprintMaskComponent>(gloves))
            return true;

        return false;
    }

    /// <summary>
    /// Sets the allowed fingerprints for a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void SetAllowedFingerprints(Entity<FingerprintReaderComponent> target, HashSet<string> fingerprints)
    {
        target.Comp.AllowedFingerprints = fingerprints;
        RaiseFingerprintReaderChangedEvent(target);
    }

    /// <summary>
    /// Adds an allowed fingerprint to a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void AddAllowedFingerprint(Entity<FingerprintReaderComponent> target, string fingerprint)
    {
        target.Comp.AllowedFingerprints.Add(fingerprint);
        RaiseFingerprintReaderChangedEvent(target);
    }

    /// <summary>
    /// Removes an allowed fingerprint from a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void RemoveAllowedFingerprint(Entity<FingerprintReaderComponent> target, string fingerprint)
    {
        target.Comp.AllowedFingerprints.Remove(fingerprint);
        RaiseFingerprintReaderChangedEvent(target);
    }

    private void RaiseFingerprintReaderChangedEvent(EntityUid uid)
    {
        var ev = new FingerprintReaderConfigurationChangedEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}
