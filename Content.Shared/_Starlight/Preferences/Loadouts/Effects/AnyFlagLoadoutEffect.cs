using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Starlight;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Uses a <see cref="LoadoutEffectGroupPrototype"/> prototype as a singular effect that can be re-used.
/// </summary>
public sealed partial class AnyFlagLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<PlayerFlags> Flags = [];
    
    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session,
        IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();
        if (session is not null && IoCManager.Resolve<ISharedPlayersRoleManager>().HasAnyPlayerFlags(session, Flags))
            return true;

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
            "any-role-required",
            ("roles", string.Join(", ", Flags.Select(Enum.GetName)))));
        return false;
    }
}