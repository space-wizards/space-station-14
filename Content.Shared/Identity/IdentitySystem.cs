using Content.Shared.Access.Systems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Ghost;
using Content.Shared.Identity.Components;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Enums;

namespace Content.Shared.Identity;

public sealed partial class IdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    private static string SlotName = "identity";

    public override void Initialize()
    {
        base.Initialize();

        InitializeEvents();
    }

    /// <summary>
    ///     Returns the name that should be used for this entity for identity purposes.
    /// </summary>
    public string IdentityName(EntityUid uid, EntityUid? viewer)
    {
        if (TryComp<IdentityComponent>(uid, out var identity))
        {
            var ident = identity.IdentityEntitySlot.ContainedEntity;
            if (ident is null)
                return Name(uid);

            if (viewer == null || !CanSeeThroughIdentity(uid, viewer.Value))
            {
                return Name(ident.Value);
            }

            return Name(uid) + $" ({Name(ident.Value)}";
        }

        return Name(uid);
    }

    public bool CanSeeThroughIdentity(EntityUid uid, EntityUid viewer)
    {
        if (uid == viewer)
            return true;

        return HasComp<SharedGhostComponent>(viewer);
    }

    #region Private API

    /// <summary>
    ///     Updates the metadata name for the id(entity) from the current state of the character.
    /// </summary>
    private void UpdateIdentityName(EntityUid uid, IdentityComponent identity)
    {
        if (identity.IdentityEntitySlot.ContainedEntity is not { } ident)
            return;

        var name = GetIdentityName(uid);
        MetaData(ident).EntityName = name;
    }

    private string GetIdentityName(EntityUid target,
        InventoryComponent? inventory=null,
        HumanoidAppearanceComponent? appearance=null)
    {
        var representation = GetIdentityRepresentation(target, inventory, appearance);
        var ev = new SeeIdentityAttemptEvent();

        RaiseLocalEvent(target, ev);
        return representation.ToStringKnown(!ev.Cancelled);
    }

    /// <summary>
    ///     Gets an 'identity representation' of an entity, with their true name being the entity name
    ///     and their 'presumed name' and 'presumed job' being the name/job on their ID card, if they have one.
    /// </summary>
    private IdentityRepresentation GetIdentityRepresentation(EntityUid target,
        InventoryComponent? inventory=null,
        HumanoidAppearanceComponent? appearance=null)
    {
        int age = HumanoidCharacterProfile.MinimumAge;
        Gender gender = Gender.Neuter;

        // Always use their actual age and gender, since that can't really be changed by an ID.
        if (Resolve(target, ref appearance, false))
        {
            gender = appearance.Gender;
            age = appearance.Age;
        }

        var trueName = Name(target);
        if (!Resolve(target, ref inventory, false))
            return new(trueName, age, gender, string.Empty);

        string? presumedJob = null;
        string? presumedName = null;

        // Get their name and job from their ID for their presumed name.
        if (_idCard.TryFindIdCard(target, out var id))
        {
            presumedName = id.FullName;
            presumedJob = id.JobTitle?.ToLowerInvariant();
        }

        // If it didn't find a job, that's fine.
        return new(trueName, age, gender, presumedName, presumedJob);
    }

    #endregion
}

public static class IdentitySystemExtensions
{
    /// <summary>
    ///     Returns the entity that should be used for identity purposes, for example to pass into localization.
    ///     This is an extension method because of its simplicity, and if it was any harder to call it might not
    ///     be used enough.
    /// </summary>
    public static EntityUid IdentityEntity(this EntitySystem sys, EntityUid uid, EntityManager ent)
    {
        if (!ent.TryGetComponent<IdentityComponent>(uid, out var identity))
            return uid;

        return identity.IdentityEntitySlot.ContainedEntity ?? uid;
    }
}


/// <summary>
///     A data structure representing the 'identity' of an entity as presented to
///     other players.
/// </summary>
public sealed class IdentityRepresentation
{
    public string TrueName;
    public int TrueAge;
    public Gender TrueGender;

    public string? PresumedName;
    public string? PresumedJob;

    public IdentityRepresentation(string trueName, int trueAge, Gender trueGender, string? presumedName=null, string? presumedJob=null)
    {
        TrueName = trueName;
        TrueAge = trueAge;
        TrueGender = trueGender;

        PresumedJob = presumedJob;
        PresumedName = presumedName;
    }

    public string ToStringKnown(bool trueName)
    {
        return trueName
            ? TrueName
            : PresumedName ?? ToStringUnknown();
    }

    /// <summary>
    ///     Returns a string representing their identity where it is 'unknown' by a viewer.
    ///     Used for cases where the viewer is not necessarily able to accurately assess
    ///     the identity of the person being viewed.
    /// </summary>
    public string ToStringUnknown()
    {
        var ageString = TrueAge switch
        {
            <= 30 => Loc.GetString("identity-age-young"),
            > 30 and <= 60 => Loc.GetString("identity-age-middle-aged"),
            > 60 => Loc.GetString("identity-age-old")
        };

        var genderString = TrueGender switch
        {
            Gender.Female => Loc.GetString("identity-gender-feminine"),
            Gender.Male => Loc.GetString("identity-gender-masculine"),
            Gender.Epicene or Gender.Neuter or _ => Loc.GetString("identity-gender-person")
        };

        // i.e. 'young assistant man' or 'old cargo technician person' or 'middle-aged captain'
        return PresumedJob is null
            ? $"{ageString} {genderString}"
            : $"{ageString} {PresumedJob} {genderString}";
    }
}
