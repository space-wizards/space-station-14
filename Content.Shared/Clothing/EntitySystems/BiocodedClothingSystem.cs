using Content.Shared.Clothing.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class BiocodedClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiocodedClothingComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BiocodedClothingComponent, GetVerbsEvent<EquipmentVerb>>(AddVerb);
        SubscribeLocalEvent<BiocodedClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<BiocodedClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<BiocodedClothingComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<BiocodedClothingComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    private void OnExamine(EntityUid uid, BiocodedClothingComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // print current armor status
        string examineMsg;
        if (!component.Enabled)
        {
            examineMsg = "biocoded-clothing-component-examine-disabled";
        }
        else
        {
            if (component.BiocodedOwner == null)
                examineMsg = "biocoded-clothing-component-examine-no-owner";
            else if (IsValidOwner(args.Examiner, uid, component))
                examineMsg = "biocoded-clothing-component-examine-owner";
            else
                examineMsg = "biocoded-clothing-component-examine-wrong-owner";
        }
        args.PushMarkup(Loc.GetString(examineMsg));
    }

    private void AddVerb(EntityUid uid, BiocodedClothingComponent component, GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // only owner can toggle biocoding on/off
        if (!IsValidOwner(args.User, args.Target, component))
            return;

        // add verb to toggle biocoding
        var msg = component.Enabled
            ? "biocoded-clothing-component-verb-disable"
            : "biocoded-clothing-component-verb-enable";
        var iconPath = component.Enabled
            ? "/Textures/Interface/VerbIcons/unlock.svg.192dpi.png"
            : "/Textures/Interface/VerbIcons/lock.svg.192dpi.png";
        args.Verbs.Add(new EquipmentVerb
        {
            Text = Loc.GetString(msg),
            Icon = new SpriteSpecifier.Texture(new ResPath(iconPath)),
            Act = () => EnableBiocoding(uid, !component.Enabled, component)
        });
    }

    private void OnGotEquipped(EntityUid uid, BiocodedClothingComponent component, GotEquippedEvent args)
    {
        // save current wearer
        component.CurrentWearer = args.Equipee;
        // save owner if it was equipped first time
        component.BiocodedOwner ??= args.Equipee;
        Dirty(component);
    }

    private void OnGotUnequipped(EntityUid uid, BiocodedClothingComponent component, GotUnequippedEvent args)
    {
        // reset current wearer
        component.CurrentWearer = null;
        Dirty(component);
    }

    private void OnEquipAttempt(EntityUid uid, BiocodedClothingComponent component, BeingEquippedAttemptEvent args)
    {
        var isValid = IsValidOwner(args.EquipTarget, uid, component);
        if (!isValid)
        {
            args.Reason = "biocoded-clothing-component-equip-failed";
            args.Cancel();
        }
    }

    private void OnGotEmagged(EntityUid uid, BiocodedClothingComponent component, ref GotEmaggedEvent args)
    {
        args.Repeatable = true;
        args.Handled = true;
        EnableBiocoding(uid, false, component);
    }

    public void EnableBiocoding(EntityUid uid, bool isEnabled, BiocodedClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!isEnabled)
        {
            // reset biocoding, forget about current owner
            component.BiocodedOwner = null;
        }
        else
        {
            // set current wearer as a new owner
            component.BiocodedOwner = component.CurrentWearer;
        }

        component.Enabled = isEnabled;
        Dirty(component);
    }

    public bool IsValidOwner(EntityUid ownerUid, EntityUid itemUid, BiocodedClothingComponent? component = null)
    {
        if (!Resolve(itemUid, ref component))
            return false;

        // not enabled - you can wear it
        if (!component.Enabled)
            return true;
        // no owner - you can wear it
        if (component.BiocodedOwner == null)
            return true;

        // has owner - check if current equipee is owner
        return component.BiocodedOwner == ownerUid;
    }
}
