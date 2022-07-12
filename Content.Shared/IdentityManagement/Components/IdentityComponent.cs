using Robust.Shared.Containers;
using Robust.Shared.Enums;

namespace Content.Shared.IdentityManagement.Components;

/// <summary>
///     Stores the identity entity (whose name is the users 'identity', etc)
///     for a given entity, and marks that it can have an identity at all.
/// </summary>
/// <remarks>
///     This is a <see cref="ContainerSlot"/> and not just a datum entity because we do sort of care that it gets deleted and sent with the user.
/// </remarks>
[RegisterComponent]
public sealed class IdentityComponent : Component
{
    [ViewVariables]
    public ContainerSlot IdentityEntitySlot = default!;
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
