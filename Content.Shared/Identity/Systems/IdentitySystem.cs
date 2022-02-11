using Content.Shared.Access.Systems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Enums;

namespace Content.Shared.Identity.Systems;

/// <summary>
///     Handles getting an entity's 'identity', essentially an IC version of <see cref="MetaDataComponent.EntityName"/>
/// </summary>
public sealed partial class IdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _sharedIdCardSystem = default!;

    /// <summary>
    ///     Gets an 'identity string' representing an entity's name from the viewer's perspective.
    /// </summary>
    /// <param name="target">The entity to get an identity string for.</param>
    /// <param name="viewer">The "viewer" of the identity.</param>
    /// <param name="useTrueName">Should we get the entity's true name, if it's known?</param>
    /// <param name="inventory">Resolve comp</param>
    /// <param name="appearance">Resolve comp</param>
    public string GetIdentityString(EntityUid target, EntityUid? viewer=null, bool useTrueName=false,
        InventoryComponent? inventory=null,
        HumanoidAppearanceComponent? appearance=null)
    {
        if (!Resolve(target, ref inventory, false))
            return Name(target);

        // These events handles things like masks blocking identity, or monkeys
        // not knowing identity.

        var viewerEv = new ShouldKnowIdentityAttemptEvent(target);
        // What the viewer says
        if (viewer != null)
        {
            RaiseLocalEvent(viewer.Value, viewerEv, false);
        }

        var targetEv = new CanKnowIdentityAttemptEvent(viewer);
        RaiseLocalEvent(target, targetEv, false);

        var trueName = useTrueName || viewerEv.KnowsTrueName || targetEv.KnowsTrueName;
        var representation = GetIdentityRepresentation(target, inventory, appearance);

        string presumedString;
        if ((viewerEv.Cancelled || targetEv.Cancelled))
        {
            presumedString = representation.ToStringKnown(false);
        }
        else
        {
            presumedString = representation.ToStringKnown(true);
        }

        if ((AlwaysKnowsIdentity(target, viewer) || trueName)
            && presumedString != representation.ToStringKnown(true))
        {
            return presumedString + $" ({representation.ToStringKnown(true)})";
        }

        return presumedString;
    }

    /// <summary>
    ///     Gets an 'identity representation' of an entity, with their true name being the entity name
    ///     and their 'presumed name' and 'presumed job' being the name/job on their ID card, if they have one.
    /// </summary>
    /// <param name="target">The entity to get an identity string for.</param>
    /// <param name="inventory">Resolve comp</param>
    /// <param name="appearance">Resolve comp</param>
    public IdentityRepresentation GetIdentityRepresentation(EntityUid target,
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
        if (_sharedIdCardSystem.TryFindIdCard(target, out var id))
        {
            presumedName = id.FullName;
            presumedJob = id.JobTitle?.ToLowerInvariant();
        }

        // If it didn't find a job, that's fine.
        return new(trueName, age, gender, presumedName, presumedJob);
    }

    /// <summary>
    ///     Should the viewer always know someone's identity,
    ///     regardless of anything else?
    /// </summary>
    private bool AlwaysKnowsIdentity(EntityUid target, EntityUid? viewer)
    {
        // Well, duh.
        if (target == viewer)
            return true;

        // Ghosts always know someone's true identity.
        if (HasComp<SharedGhostComponent>(viewer))
            return true;

        return false;
    }
}

public abstract class IdentityAttemptEventBase : CancellableEntityEventArgs
{
    /// <summary>
    ///     Should the viewing entity be able to know the true name of the target?
    /// </summary>
    public bool KnowsTrueName = false;
}

/// <summary>
///     Raised on an entity to determine if it's able to 'know' who the target entity is.
///     Useful for things like how monkeys/drones shouldn't know who people are, or people on 'opposing factions'
///     shouldn't know either.
/// </summary>
public sealed class ShouldKnowIdentityAttemptEvent : IdentityAttemptEventBase
{
    public EntityUid Target;

    public ShouldKnowIdentityAttemptEvent(EntityUid target)
    {
        Target = target;
    }
}

/// <summary>
///     Raised on an entity to determine if the viewer should be able to know who it is, identity-wise.
///     Useful for things like masks blocking identity.
/// </summary>
public sealed class CanKnowIdentityAttemptEvent : IdentityAttemptEventBase, IInventoryRelayEvent
{
    public EntityUid? Viewer;

    // i.e. masks or helmets.
    public SlotFlags TargetSlots => SlotFlags.MASK | SlotFlags.HEAD;

    public CanKnowIdentityAttemptEvent(EntityUid? viewer)
    {
        Viewer = viewer;
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
