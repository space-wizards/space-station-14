using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;

namespace Content.Shared.IdentityManagement.Components;

/// <summary>
///     Stores the identity entity (whose name is the users 'identity', etc)
///     for a given entity, and marks that it can have an identity at all.
/// </summary>
/// <remarks>
///     This is a <see cref="ContainerSlot"/> and not just a datum entity because we do sort of care that it gets deleted and sent with the user.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class IdentityComponent : Component
{
    /// <summary>
    /// The slot which carries around the entity representing the carrier's
    /// perceived identity. May be null if the component is not initialized.
    /// </summary>
    [ViewVariables]
    public ContainerSlot? IdentityEntitySlot;
}

/// <summary>
///     A data structure representing the 'identity' of an entity as presented to
///     other players.
/// </summary>
public sealed class IdentityRepresentation
{
    public string TrueName;
    public Gender TrueGender;

    public string AgeString;

    public string? PresumedName;
    public string? PresumedJob;

    public IdentityRepresentation(string trueName, Gender trueGender, string ageString, string? presumedName = null, string? presumedJob = null)
    {
        TrueName = trueName;
        TrueGender = trueGender;

        AgeString = ageString;

        PresumedJob = presumedJob;
        PresumedName = presumedName;
    }

    /// <summary>
    /// Get this identity as a string
    /// </summary>
    /// <param name="trueName">Should we show their "true" name or hide it?</param>
    /// <param name="nameOverride">A "true name" override</param>
    /// <returns></returns>
    public string ToStringKnown(bool trueName, string? nameOverride)
    {
        return trueName
            ? nameOverride ?? TrueName
            : PresumedName ?? ToStringUnknown();
    }

    /// <summary>
    ///     Returns a string representing their identity where it is 'unknown' by a viewer.
    ///     Used for cases where the viewer is not necessarily able to accurately assess
    ///     the identity of the person being viewed.
    /// </summary>
    public string ToStringUnknown()
    {
        var genderString = TrueGender switch
        {
            Gender.Female => Loc.GetString("identity-gender-feminine"),
            Gender.Male => Loc.GetString("identity-gender-masculine"),
            Gender.Epicene or Gender.Neuter or _ => Loc.GetString("identity-gender-person")
        };

        // i.e. 'young assistant man' or 'old cargo technician person' or 'middle-aged captain'
        return PresumedJob is null
            ? $"{AgeString} {genderString}"
            : $"{AgeString} {PresumedJob} {genderString}";
    }
}
