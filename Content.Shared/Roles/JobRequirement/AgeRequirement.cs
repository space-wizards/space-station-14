using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

/// <summary>
/// Requires the character to be older or younger than a certain age (inclusive)
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AgeRequirement : JobRequirement
{
    [DataField(required: true)]
    public int RequiredAge;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage details)
    {
        details = new FormattedMessage();

        if (profile is null) //the profile could be null if the player is a ghost. In this case we don't need to block the role selection for ghostrole
            return true;

        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-age-young-enough" : "role-timer-age-old-enough",
            ("age", RequiredAge)));

        if (!Inverted)
        {
            if (profile.Age >= RequiredAge)
                return true;

            details = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-age-not-old-enough",
                ("age", RequiredAge)));
            return false;
        }

        if (profile.Age <= RequiredAge)
            return true;

        details = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-age-not-young-enough",
            ("age", RequiredAge)));
        return false;
    }
}
