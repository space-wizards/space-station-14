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
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (profile is null) //the profile could be null if the player is a ghost. In this case we don't need to block the role selection for ghostrole
            return true;

        var sb = new StringBuilder();
        sb.Append("[color=yellow]");
        foreach (var s in Species)
        {
            sb.Append(Loc.GetString(protoManager.Index(s).Name) + " ");
        }

        sb.Append("[/color]");

        if (!Inverted)
        {
            reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-whitelisted-species")}\n{sb}");

            if (!Species.Contains(profile.Species))
                return false;
        }
        else
        {
            reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-blacklisted-species")}\n{sb}");

            if (Species.Contains(profile.Species))
                return false;
        }

        return true;
    }
}
