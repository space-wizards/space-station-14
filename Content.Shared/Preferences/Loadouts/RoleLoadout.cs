using System.Diagnostics.CodeAnalysis;
using Content.Shared.Random;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Contains all of the selected data for a role's loadout.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoleLoadout
{
    public readonly ProtoId<RoleLoadoutPrototype> Role;

    public Dictionary<ProtoId<LoadoutGroupPrototype>, List<Loadout>> SelectedLoadouts = new();

    public RoleLoadout(ProtoId<RoleLoadoutPrototype> role)
    {
        Role = role;
    }

    /// <summary>
    /// Ensures all prototypes exist and effects can be applied.
    /// </summary>
    public void EnsureValid(ICommonSession session, IDependencyCollection collection)
    {
        var groupRemove = new ValueList<string>();
        var protoManager = collection.Resolve<IPrototypeManager>();

        foreach (var (group, groupLoadouts) in SelectedLoadouts)
        {
            // Dump if Group doesn't exist
            if (!protoManager.TryIndex(group, out var groupProto))
            {
                groupRemove.Add(group);
                continue;
            }

            var loadouts = groupLoadouts[..Math.Min(groupLoadouts.Count, groupProto.MaxLimit)];

            // Validate first
            for (var i = loadouts.Count - 1; i >= 0; i--)
            {
                var loadout = loadouts[i];

                // Validate the loadout can be applied (e.g. points).
                if (!IsValid(session, loadout.Prototype, collection, out _))
                {
                    loadouts.RemoveAt(i);
                }
            }

            // Apply defaults if required
            // Technically it's possible for someone to game themselves into loadouts they shouldn't have
            // If you put invalid ones first but that's your fault for not using sensible defaults
            if (loadouts.Count < groupProto.MinLimit)
            {
                for (var i = 0; i < Math.Min(groupProto.MinLimit, groupProto.Loadouts.Count); i++)
                {
                    var defaultLoadout = new Loadout()
                    {
                        Prototype = groupProto.Loadouts[i],
                    };

                    if (loadouts.Contains(defaultLoadout))
                        continue;

                    SelectedLoadouts[group].Add(defaultLoadout);
                }
            }

            SelectedLoadouts[group] = loadouts;
        }

        foreach (var value in groupRemove)
        {
            SelectedLoadouts.Remove(value);
        }
    }

    /// <summary>
    /// Resets the selected loadouts to default if no data is present.
    /// </summary>
    public void SetDefault(IEntityManager entManager, IPrototypeManager protoManager, bool force = false)
    {
        if (force)
            SelectedLoadouts.Clear();

        var roleProto = protoManager.Index(Role);

        for (var i = roleProto.Groups.Count - 1; i >= 0; i--)
        {
            var group = roleProto.Groups[i];

            if (!protoManager.TryIndex(group, out var groupProto))
                continue;

            if (SelectedLoadouts.ContainsKey(group))
                continue;

            SelectedLoadouts[group] = new List<Loadout>();

            if (groupProto.MinLimit > 0)
            {
                // Apply any loadouts we can.
                for (var j = 0; j < Math.Min(groupProto.MinLimit, groupProto.Loadouts.Count); j++)
                {
                    AddLoadout(group, groupProto.Loadouts[j], protoManager);
                }
            }
        }
    }

    /// <summary>
    /// Returns whether a loadout is valid or not.
    /// </summary>
    public bool IsValid(ICommonSession session, ProtoId<LoadoutPrototype> loadout, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var protoManager = collection.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex(loadout, out var loadoutProto))
        {
            // Uhh
            reason = FormattedMessage.FromMarkup("");
            return false;
        }

        foreach (var effect in loadoutProto.Effects)
        {
            if (!effect.Validate(session, collection!, out reason))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Applies the specified loadout to this group.
    /// </summary>
    public bool AddLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype> selectedLoadout, IPrototypeManager protoManager)
    {
        var groupLoadouts = SelectedLoadouts[selectedGroup];

        // Need to unselect existing ones if we're at or above limit
        var limit = Math.Max(0, groupLoadouts.Count + 1 - protoManager.Index(selectedGroup).MaxLimit);

        for (var i = 0; i < groupLoadouts.Count; i++)
        {
            var loadout = groupLoadouts[i];

            if (loadout.Prototype != selectedLoadout)
            {
                // Remove any other loadouts that might push it above the limit.
                if (limit > 0)
                {
                    limit--;
                    groupLoadouts.RemoveAt(i);
                    i--;
                }

                continue;
            }

            DebugTools.Assert(false);
            return false;
        }

        groupLoadouts.Add(new Loadout()
        {
            Prototype = selectedLoadout,
        });

        return true;
    }

    /// <summary>
    /// Removed the specified loadout from this group.
    /// </summary>
    public bool RemoveLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype> selectedLoadout, IPrototypeManager protoManager)
    {
        var groupLoadouts = SelectedLoadouts[selectedGroup];

        // If removal takes it below minimum then refuse it
        if (protoManager.TryIndex(selectedGroup, out var groupProto))
        {
            if (groupLoadouts.Count <= groupProto.MinLimit)
                return false;
        }

        for (var i = 0; i < groupLoadouts.Count; i++)
        {
            var loadout = groupLoadouts[i];

            if (loadout.Prototype != selectedLoadout)
                continue;

            groupLoadouts.RemoveAt(i);
            return true;
        }

        return false;
    }
}
