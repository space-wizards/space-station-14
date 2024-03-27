using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

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

        foreach (var (group, loadout) in SelectedLoadouts)
        {
            // Dump if Group doesn't exist
            if (!protoManager.TryIndex(group, out var groupProto))
            {
                groupRemove.Add(group);
                continue;
            }

            // Set to default if
            // - Group isn't optional and the selection is optional
            // - Loadout doesn't exist
            // This can fail if the first one isn't possible due to some effect but this is just a fallback.
            if (!groupProto.Optional && (loadout == null || !protoManager.HasIndex(loadout.Value)))
            {
                SelectedLoadouts[group] =
                    groupProto.Loadouts.Count > 0 ? groupProto.Loadouts[0] : loadout;
                continue;
            }

            // Validate the loadout can be applied (e.g. points).
            if (loadout != null)
            {
                if (!IsValid(session, loadout, collection, out _))
                {
                    SelectedLoadouts[group] =
                        groupProto.Loadouts.Count > 0 ? groupProto.Loadouts[0] : loadout;
                }
            }
        }

        foreach (var value in groupRemove)
        {
            SelectedLoadouts.Remove(value);
        }
    }

    /// <summary>
    /// Resets the selected loadouts to default.
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
            {
                roleProto.Groups.RemoveAt(i);
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

            if (SelectedLoadouts.ContainsKey(group))
                continue;

            ApplyLoadout(group, selected, entManager);
        }
    }

    /// <summary>
    /// Returns whether a loadout is valid or not.
    /// </summary>
    public bool IsValid(ICommonSession session, ProtoId<LoadoutPrototype>? loadoutId, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (loadoutId == null)
            return true;

        var protoManager = collection.Resolve<IPrototypeManager>();

        if (!protoManager.TryIndex(loadoutId.Value, out var loadoutProto))
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
    public void ApplyLoadout(ProtoId<LoadoutGroupPrototype> selectedGroup, ProtoId<LoadoutPrototype>? selectedLoadout, IEntityManager entManager)
    {
        SelectedLoadouts[selectedGroup] = selectedLoadout;

        var ev = new RoleLoadoutUpdatedEvent();
        entManager.EventBus.RaiseEvent(EventSource.Local, ref ev);
    }
}
