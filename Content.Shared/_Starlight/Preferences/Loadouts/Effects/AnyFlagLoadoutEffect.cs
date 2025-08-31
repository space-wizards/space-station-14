using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._NullLink;
using Content.Shared.Starlight;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Takes a list of PlayerFlags and checks if the player has any of them. 
/// </summary>
public sealed partial class RolesReqLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public ProtoId<RoleRequirementPrototype> Proto = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session,
        IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var requirement = IoCManager.Resolve<IPrototypeManager>().Index(Proto);

        reason = new FormattedMessage();
        if (session is not null && IoCManager.Resolve<ISharedNullLinkPlayerRolesReqManager>().IsAnyRole(session, requirement.Roles))
            return true;

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            "roles-req-any-role-required",
            ("discord", Loc.GetString(requirement.Discord)),
            ("roles", Loc.GetString(requirement.RolesLoc))));
        return false;
    }
}