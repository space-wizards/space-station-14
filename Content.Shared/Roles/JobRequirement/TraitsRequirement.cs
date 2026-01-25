using System.Diagnostics.CodeAnalysis;
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
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (profile is null) //the profile could be null if the player is a ghost. In this case we don't need to block the role selection for ghostrole
            return true;

        var sb = new StringBuilder();
        sb.Append("[color=yellow]");
        foreach (var t in Traits)
        {
            sb.Append(Loc.GetString(protoManager.Index(t).Name) + " ");
        }

        sb.Append("[/color]");

        if (!Inverted)
        {
            reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-whitelisted-traits")}\n{sb}");
            //at least one of
            foreach (var trait in Traits)
            {
                if (profile.TraitPreferences.Contains(trait))
                    return true;
            }
            return false;
        }
        else
        {
            reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-blacklisted-traits")}\n{sb}");

            foreach (var trait in Traits)
            {
                if (profile.TraitPreferences.Contains(trait))
                    return false;
            }
        }

        return true;
    }
}
