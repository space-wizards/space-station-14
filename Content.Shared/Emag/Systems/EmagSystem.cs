using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Emag.Systems;

/// How to add an emag interaction:
/// 1. Go to the system for the component you want the interaction with
/// 2. Subscribe to the GotEmaggedEvent
/// 3. Have some check for if this actually needs to be emagged or is already emagged (to stop charge waste)
/// 4. Past the check, add all the effects you desire and HANDLE THE EVENT ARGUMENT so a charge is spent
/// 5. Optionally, set Repeatable on the event to true if you don't want the emagged component to be added
public sealed class EmagSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<EmaggedComponent, OnAccessOverriderAccessUpdatedEvent>(OnAccessOverriderAccessUpdated);
    }

    private void OnAccessOverriderAccessUpdated(Entity<EmaggedComponent> entity, ref OnAccessOverriderAccessUpdatedEvent args)
    {
        if (!CompareFlag(entity.Comp.EmagType, EmagType.Access))
            return;

        entity.Comp.EmagType &= ~EmagType.Access;
        Dirty(entity);
    }
    private void OnAfterInteract(EntityUid uid, EmagComponent comp, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryEmagEffect((uid, comp), args.User, target);
    }

    /// <summary>
    /// Does the emag effect on a specified entity
    /// </summary>
    public bool TryEmagEffect(Entity<EmagComponent?> ent, EntityUid user, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (_tag.HasTag(target, ent.Comp.EmagImmuneTag))
            return false;

        Entity<LimitedChargesComponent?> chargesEnt = ent.Owner;
        if (_sharedCharges.IsEmpty(chargesEnt))
        {
            _popup.PopupClient(Loc.GetString("emag-no-charges"), user, user);
            return false;
        }

        var emaggedEvent = new GotEmaggedEvent(user, ent.Comp.EmagType);
        RaiseLocalEvent(target, ref emaggedEvent);

        if (!emaggedEvent.Handled)
            return false;

        _popup.PopupPredicted(Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))), user, user, PopupType.Medium);

        _audio.PlayPredicted(ent.Comp.EmagSound, ent, ent);

        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} emagged {ToPrettyString(target):target} with flag(s): {ent.Comp.EmagType}");

        if (emaggedEvent.Handled)
            _sharedCharges.TryUseCharge(chargesEnt);

        if (!emaggedEvent.Repeatable)
        {
            EnsureComp<EmaggedComponent>(target, out var emaggedComp);

            emaggedComp.EmagType |= ent.Comp.EmagType;
            Dirty(target, emaggedComp);
        }

        return emaggedEvent.Handled;
    }

    /// <summary>
    /// Checks whether an entity has the EmaggedComponent with a set flag.
    /// </summary>
    /// <param name="target">The target entity to check for the flag.</param>
    /// <param name="flag">The EmagType flag to check for.</param>
    /// <returns>True if entity has EmaggedComponent and the provided flag. False if the entity lacks EmaggedComponent or provided flag.</returns>
    public bool CheckFlag(EntityUid target, EmagType flag)
    {
        if (!TryComp<EmaggedComponent>(target, out var comp))
            return false;

        if ((comp.EmagType & flag) == flag)
            return true;

        return false;
    }

    /// <summary>
    /// Compares a flag to the target.
    /// </summary>
    /// <param name="target">The target flag to check.</param>
    /// <param name="flag">The flag to check for within the target.</param>
    /// <returns>True if target contains flag. Otherwise false.</returns>
    public bool CompareFlag(EmagType target, EmagType flag)
    {
        if ((target & flag) == flag)
            return true;

        return false;
    }
}


[Flags]
[Serializable, NetSerializable]
public enum EmagType : byte
{
    None = 0,
    Interaction = 1 << 1,
    Access = 1 << 2
}
/// <summary>
/// Shows a popup to emag user (client side only!) and adds <see cref="EmaggedComponent"/> to the entity when handled
/// </summary>
/// <param name="UserUid">Emag user</param>
/// <param name="Type">The emag type to use</param>
/// <param name="Handled">Did the emagging succeed? Causes a user-only popup to show on client side</param>
/// <param name="Repeatable">Can the entity be emagged more than once? Prevents adding of <see cref="EmaggedComponent"/></param>
/// <remarks>Needs to be handled in shared/client, not just the server, to actually show the emagging popup</remarks>
[ByRefEvent]
public record struct GotEmaggedEvent(EntityUid UserUid, EmagType Type, bool Handled = false, bool Repeatable = false);
