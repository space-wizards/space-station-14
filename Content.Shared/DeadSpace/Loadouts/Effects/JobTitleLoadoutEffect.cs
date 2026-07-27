// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences;
using Content.Shared.StatusIcon;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.DeadSpace.Loadouts.Effects;

public sealed partial class JobTitleLoadoutEffect : LoadoutEffect
{
    
    [DataField]
    public LocId Title;

    [DataField]
    public ProtoId<JobIconPrototype>? Icon;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        return true;
    }

}