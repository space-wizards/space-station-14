using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

/// <summary>
/// Requires a character to have, or not have, certain traits
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class TraitsRequirement : JobRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<TraitPrototype>> Traits = new();

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        out FormattedMessage details)
    {
        details = new FormattedMessage();

        if (profile is null) //the profile could be null if the player is a ghost. In this case we don't need to block the role selection for ghostrole
            return true;

        var sb = new StringBuilder();
        foreach (var t in Traits)
        {
            sb.Append(Loc.GetString(protoManager.Index(t).Name) + " ");
        }

        // Default message is success.
        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-blacklisted-traits-pass" : "role-timer-whitelisted-traits-pass",
            ("traits", sb)));

        var hasAnyTrait = Traits.Any(trait => profile.TraitPreferences.Contains(trait));

        // !Inverted = Whitelist mode, meaning player must have ONE of the traits.
        // Inverted = Blacklist mode, meaning player must have NONE of the traits.
        if (!Inverted == hasAnyTrait)
            return true;

        // Change to fail message.
        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-blacklisted-traits-fail" : "role-timer-whitelisted-traits-fail",
            ("traits", sb)));
        return false;
    }
}
