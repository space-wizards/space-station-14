using Content.Shared.Body.Components;

namespace Content.Shared.Body.Systems.Part;

/// <summary>
///     Contains utility functions for getting information about a body part.
/// </summary>
public abstract partial class SharedBodyPartSystem
{
    /// <summary>
    ///     Returns the current size of this part, based on
    ///     the added up size of its mechanisms.
    /// </summary>
    public int GetCurrentSize(EntityUid uid, SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return 0;

        int size = 0;
        foreach (var mech in GetAllMechanisms(uid, part))
        {
            size += mech.Size;
        }

        return size;
    }

    /// <summary>
    ///     Checks if the mechanism can be added the body part.
    ///     Currently checks compatibility and size
    /// </summary>
    /// <remarks></remarks>
    public bool CanAddMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (!IsCompatible(uid, mechanism, part))
            return false;

        if (!(GetCurrentSize(uid, part) + mechanism.Size <= part.Size))
            return false;

        return part.MechanismContainer?.CanInsert(mechanism.Owner, EntityManager) ?? false;
    }

    public bool IsCompatible(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (mechanism.Compatibility == null || part.Compatibility == null)
            return true;

        return (mechanism.Compatibility == part.Compatibility);
    }

    /// <summary>
    ///     Checks if the mechanism can be removed from the body part.
    ///     Currently just checks if the entity can be removed from the container
    ///     as there are no other constraints for removal.
    /// </summary>
    public bool CanRemoveMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        return part.MechanismContainer?.CanRemove(mechanism.Owner, EntityManager) ?? false;
    }
}
