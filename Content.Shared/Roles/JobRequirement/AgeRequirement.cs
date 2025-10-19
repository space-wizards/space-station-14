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
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (profile is null) //the profile could be null if the player is a ghost. In this case we don't need to block the role selection for ghostrole
            return true;

        if (!Inverted)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-age-too-young",
                ("age", RequiredAge)));

            if (profile.Age < RequiredAge)
                return false;
        }
        else
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-age-too-old",
                ("age", RequiredAge)));

            if (profile.Age > RequiredAge)
                return false;
        }

        return true;
    }
}
