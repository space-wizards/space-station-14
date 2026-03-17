using System.Diagnostics.CodeAnalysis;
using Content.Shared.Machines.Components;

namespace Content.Shared.Machines.EntitySystems;

/// <summary>
/// Shared handling of multipart machines.
/// </summary>
public abstract class SharedMultipartMachineSystem : EntitySystem
{
    protected EntityQuery<TransformComponent> XformQuery;

    public override void Initialize()
    {
        base.Initialize();

        XformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    /// Returns whether each non-optional part of the machine has a matched entity
    /// </summary>
    /// <param name="ent">Entity to check the assembled state of.</param>
    /// <returns>True if all non-optional parts have a matching entity, false otherwise.</returns>
    public bool IsAssembled(Entity<MultipartMachineComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        foreach (var part in ent.Comp.Parts.Values)
        {
            if (!part.Entity.HasValue && !part.Optional)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether a machine has a specifed EntityUid bound to one of its parts.
    /// </summary>
    /// <param name="machine">Entity, which might have a multpart machine attached, to use for the query.</param>
    /// <param name="entity">EntityUid to search for.</param>
    /// <returns>True if any part has the specified EntityUid, false otherwise.</returns>
    public bool HasPartEntity(Entity<MultipartMachineComponent?> machine, EntityUid entity)
    {
        if (!Resolve(machine, ref machine.Comp))
            return false;

        foreach (var part in machine.Comp.Parts.Values)
        {
            if (part.Entity.HasValue && part.Entity.Value == entity)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get the EntityUid for the entity bound to a specific part, if one exists.
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query.</param>
    /// <param name="part">Enum value for the part to find, must match the value specified in YAML.</param>
    /// <returns>May contain the resolved EntityUid for the specified part, null otherwise.</returns>
    public EntityUid? GetPartEntity(Entity<MultipartMachineComponent?> ent, Enum part)
    {
        if (!TryGetPartEntity(ent, part, out var entity))
            return null;

        return entity;
    }

    /// <summary>
    /// Get the EntityUid for the entity bound to a specific part, if one exists.
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query.</param>
    /// <param name="part">Enum for the part to find, must match the value specified in YAML.</param>
    /// <param name="entity">Out var which may contain the matched EntityUid for the specified part.</param>
    /// <returns>True if the part is found and has a matched entity, false otherwise.</returns>
    public bool TryGetPartEntity(
        Entity<MultipartMachineComponent?> ent,
        Enum part,
        [NotNullWhen(true)] out EntityUid? entity
    )
    {
        entity = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Parts.TryGetValue(part, out var value) && value.Entity.HasValue)
        {
            entity = value.Entity.Value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a machine has an entity bound to a specific part
    /// </summary>
    /// <param name="ent">Entity, which might have a multipart machine attached, to use for the query.</param>
    /// <param name="part">Enum for the part to find.</param>
    /// <returns>True if the specific part has a entity bound to it, false otherwise.</returns>
    public bool HasPart(Entity<MultipartMachineComponent?> ent, Enum part)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Parts.TryGetValue(part, out var value))
            return false;

        return value.Entity != null;
    }
}
