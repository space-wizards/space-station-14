using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

/// <summary>
/// Requires the character to be or not be on the list of specified species
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class SpeciesRequirement : JobRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<SpeciesPrototype>> Species = new();

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
        foreach (var s in Species)
        {
            sb.Append(Loc.GetString(protoManager.Index(s).Name) + " ");
        }

        // Default message is success.
        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-blacklisted-species-pass" : "role-timer-whitelisted-species-pass",
            ("species", sb)));

        // !Inverted = Whitelist mode, meaning player should have ONE of the species.
        // Inverted = Blacklist mode, meaning player must have NONE of those species.
        if (!Inverted == Species.Contains(profile.Species))
            return true;

        // Change to fail message.
        details = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            Inverted ? "role-timer-blacklisted-species-fail" : "role-timer-whitelisted-species-fail",
            ("species", sb)));
        return false;
    }
}
