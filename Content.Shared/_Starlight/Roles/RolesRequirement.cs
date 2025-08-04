using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared._NullLink;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class RolesRequirement : JobRequirement
{
    [DataField(required: true)]
    public ProtoId<RoleRequirementPrototype> Proto = default!;

    public override bool Check(IEntityManager entManager,
        ICommonSession? player,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan>? playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var requirement = protoManager.Index(Proto);
        reason = new FormattedMessage();
        if (player is not null && IoCManager.Resolve<ISharedNullLinkPlayerRolesReqManager>().IsAnyRole(player, requirement.Roles))
            return true;

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            "roles-req-any-role-required",
            ("discord", Loc.GetString(requirement.Discord)),
            ("roles", Loc.GetString(requirement.RolesLoc))));
        return false;
    }
}
