using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

public sealed partial class SpeciesLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<ProtoId<SpeciesPrototype>> Species = new();

    public override bool Validate(RoleLoadout loadout, ICommonSession session, IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (loadout.Species == null)
        {
            reason = null;
            return true;
        }

        if (Species.Contains(loadout.Species.Value))
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted("loadout-group-species-restriction");
        return false;
    }
}
