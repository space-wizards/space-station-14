using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Turrets;

/// <summary>
/// This system is used for validating potential targets for NPCs with a <see cref="TurretTargetSettingsComponent"/> (i.e., turrets).
/// A turret will consider an entity a valid target if the entity does not possess any access tags which appear on the
/// turret's <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list.
/// </summary>
public sealed partial class TurretTargetSettingsSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    private ProtoId<AccessLevelPrototype> _accessLevelBorg = "Borg";
    private ProtoId<AccessLevelPrototype> _accessLevelBasicSilicon = "BasicSilicon";

    // DS14-start
    private static readonly ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";
    private static readonly ProtoId<NpcFactionPrototype> CentralCommandFaction = "CentralCommand";
    // DS14-end

    /// <summary>
    /// Adds or removes access levels from a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The proto ID for the access level</param>
    /// <param name="enabled">Set 'true' to add the exemption, or 'false' to remove it</param>
    /// <param name="dirty">Set 'true' to dirty the component</param>
    [PublicAPI]
    public void SetAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ProtoId<AccessLevelPrototype> exemption, bool enabled, bool dirty = true)
    {
        if (enabled)
            ent.Comp.ExemptAccessLevels.Add(exemption);
        else
            ent.Comp.ExemptAccessLevels.Remove(exemption);

        if (dirty)
            Dirty(ent);
    }

    /// <summary>
    /// Adds or removes a collection of access levels from a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The collection of access level proto IDs to add or remove</param>
    /// <param name="enabled">Set 'true' to add the collection as exemptions, or 'false' to remove them</param>
    [PublicAPI]
    public void SetAccessLevelExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<ProtoId<AccessLevelPrototype>> exemptions, bool enabled)
    {
        foreach (var exemption in exemptions)
            SetAccessLevelExemption(ent, exemption, enabled, false);

        Dirty(ent);
    }

    /// <summary>
    /// Sets a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list to contain only a supplied collection of access levels.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemptions">The supplied collection of access level proto IDs</param>
    [PublicAPI]
    public void SyncAccessLevelExemptions(Entity<TurretTargetSettingsComponent> ent, ICollection<ProtoId<AccessLevelPrototype>> exemptions)
    {
        ent.Comp.ExemptAccessLevels.Clear();
        SetAccessLevelExemptions(ent, exemptions, true);
    }

    /// <summary>
    /// Sets a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list to match that of another.
    /// </summary>
    /// <param name="target">The entity this is having its exemption list updated <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="source">The entity that is being used as a template for the target</param>
    [PublicAPI]
    public void SyncAccessLevelExemptions(Entity<TurretTargetSettingsComponent> target, Entity<TurretTargetSettingsComponent> source)
    {
        SyncAccessLevelExemptions(target, source.Comp.ExemptAccessLevels);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains a specific access level.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemption">The access level proto ID being checked</param>
    [PublicAPI]
    public bool HasAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ProtoId<AccessLevelPrototype> exemption)
    {
        if (ent.Comp.ExemptAccessLevels.Count == 0)
            return false;

        return ent.Comp.ExemptAccessLevels.Contains(exemption);
    }

    /// <summary>
    /// Returns whether a <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list contains one or more access levels from another collection.
    /// </summary>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="exemptions"></param>
    [PublicAPI]
    public bool HasAnyAccessLevelExemption(Entity<TurretTargetSettingsComponent> ent, ICollection<ProtoId<AccessLevelPrototype>> exemptions)
    {
        if (ent.Comp.ExemptAccessLevels.Count == 0)
            return false;

        foreach (var exemption in exemptions)
        {
            if (HasAccessLevelExemption(ent, exemption))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns whether an entity is a valid target for a turret.
    /// </summary>
    /// <remarks>
    /// Returns false if the target possesses one or more access tags that are present on the entity's <see cref="TurretTargetSettingsComponent.ExemptAccessLevels"/> list.
    /// </remarks>
    /// <param name="ent">The entity and its <see cref="TurretTargetSettingsComponent"/></param>
    /// <param name="target">The target entity</param>
    [PublicAPI]
    public bool EntityIsTargetForTurret(Entity<TurretTargetSettingsComponent> ent, EntityUid target)
    {
        // DS14-start
        if (TargetIsFriendly(ent, target))
            return false;
        // DS14-end

        var accessLevels = _accessReader.FindAccessTags(target);

        if (accessLevels.Contains(_accessLevelBorg))
            return !HasAccessLevelExemption(ent, _accessLevelBorg);

        if (accessLevels.Contains(_accessLevelBasicSilicon))
            return !HasAccessLevelExemption(ent, _accessLevelBasicSilicon);

        return !HasAnyAccessLevelExemption(ent, accessLevels);
    }

    // DS14-start
    private bool TargetIsFriendly(
        Entity<TurretTargetSettingsComponent> source,
        EntityUid target)
    {
        TryComp<NpcFactionMemberComponent>(source, out var sourceFactionMember);
        TryComp<NpcFactionMemberComponent>(target, out var targetFactionMember);
        TryComp<TurretTargetSettingsComponent>(target, out var targetSettings);

        var sourceFactions = source.Comp.TurretFactions.Count > 0
            ? source.Comp.TurretFactions
            : sourceFactionMember?.Factions;
        var targetFactions = targetSettings?.TurretFactions.Count > 0
            ? targetSettings.TurretFactions
            : targetFactionMember?.Factions;

        if (sourceFactions == null || targetFactions == null)
            return false;

        if (sourceFactions.Overlaps(targetFactions))
            return true;

        return (sourceFactions.Contains(NanoTrasenFaction) && targetFactions.Contains(CentralCommandFaction)) ||
               (sourceFactions.Contains(CentralCommandFaction) && targetFactions.Contains(NanoTrasenFaction));
    }
    // DS14-end
}
