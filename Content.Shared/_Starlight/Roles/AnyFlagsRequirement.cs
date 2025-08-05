using System.Diagnostics.CodeAnalysis;
using Content.Shared.Localizations;
using Content.Shared.Starlight;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AnyFlagsRequirement : JobRequirement
{
    [DataField(required: true)]
    public List<PlayerFlags> Flags = [];

    public override bool Check(IEntityManager entManager,
        ICommonSession? player,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan>? playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        if (player is not null && IoCManager.Resolve<ISharedPlayersRoleManager>().HasAnyPlayerFlags(player, Flags))
            return true;

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            "any-role-required",
            ("roles", string.Join(", ", Flags.Select(Enum.GetName)))));
        return false;
    }
}
