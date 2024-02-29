using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Contains all of the selected data for a role's loadout.
/// </summary>
public sealed class RoleLoadout
{
    public readonly ProtoId<RoleLoadoutPrototype> Role;

    public Dictionary<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>?> SelectedLoadouts = new();

    public RoleLoadout(ProtoId<RoleLoadoutPrototype> role)
    {
        Role = role;
    }

    /// <summary>
    /// Resets the selected loadouts to default.
    /// </summary>
    /// <param name="protoManager"></param>
    public void SetDefault(IPrototypeManager protoManager)
    {
        SelectedLoadouts.Clear();
        var roleProto = protoManager.Index(Role);
        var toRemove = new ValueList<ProtoId<LoadoutGroupPrototype>>();

        foreach (var group in roleProto.Groups)
        {
            if (!protoManager.TryIndex(group, out var groupProto))
            {
                toRemove.Add(group);
                continue;
            }

            ProtoId<LoadoutPrototype>? selected;

            if (groupProto.Optional || groupProto.Loadouts.Count == 0)
            {
                selected = null;
            }
            else
            {
                selected = groupProto.Loadouts[0];
            }

            SelectedLoadouts[group] = selected;
        }
    }
}
