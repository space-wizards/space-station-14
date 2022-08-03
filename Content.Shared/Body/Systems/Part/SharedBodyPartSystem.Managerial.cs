using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems.Part;

public abstract partial class SharedBodyPartSystem
{
    /// <summary>
    /// Gets all mechanisms in the provided part, if any
    /// </summary>
    public IEnumerable<MechanismComponent> GetAllMechanisms(EntityUid uid, SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            yield break;

        if (!ContainerSystem.TryGetContainer(uid, ContainerName, out var mechanismContainer))
            yield break;

        foreach (var ent in mechanismContainer.ContainedEntities)
        {
            if (TryComp<MechanismComponent>(ent, out var mechanism))
                yield return mechanism;
        }
    }

    /// <summary>
    ///     Tries to add a <see cref="MechanismComponent" /> to this part.
    /// </summary>
    /// <param name="uid">The part to add a mechanism too.</param>
    /// <param name="mechanism">The mechanism to add.</param>
    /// <param name="force">
    ///     Whether or not to check if the mechanism is compatible.
    ///     Passing true does not guarantee it to be added, for example if
    ///     it was already added before.
    /// </param>
    /// <param name="part">Resolve comp</param>
    /// <returns>true if added, false otherwise even if it was already added.</returns>
    public bool TryAddMechanism(EntityUid uid, MechanismComponent mechanism, bool force = false,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (!force && !CanAddMechanism(uid, mechanism, part))
            return false;

        return AddMechanismAndRaiseEvents(mechanism, part);
    }

    protected bool AddMechanismAndRaiseEvents(MechanismComponent mechanism, SharedBodyPartComponent part)
    {
        if (part.MechanismContainer == null)
            return false;

        if (!part.MechanismContainer.Insert(mechanism.Owner))
            return false;

        mechanism.Part = part;

        if (part.Body == null)
            RaiseLocalEvent(mechanism.Owner, new MechanismAddedToPartEvent(part.Owner));
        else
            RaiseLocalEvent(mechanism.Owner, new MechanismAddedToPartInBodyEvent(part.Body.Owner, part.Owner));

        return true;
    }

    /// <summary>
    ///     Tries to remove the given <see cref="mechanism" /> from this part.
    /// </summary>
    /// <param name="uid">The part to remove a mechanism from</param>
    /// <param name="mechanism">The mechanism to remove.</param>
    /// <param name="part"></param>
    /// <returns>True if it was removed, false otherwise.</returns>
    public bool TryRemoveMechanism(EntityUid uid, MechanismComponent mechanism,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (!CanRemoveMechanism(uid, mechanism, part))
            return false;

        if (!RemoveMechanismAndRaiseEvents(mechanism, part))
            return false;

        mechanism.Owner.RandomOffset(0.25f);

        return true;
    }

    /// <summary>
    ///     Tries to remove the given <see cref="mechanism" /> from this
    ///     part and drops it at the specified coordinates.
    /// </summary>
    /// <param name="uid">The part to remove a mechanism from</param>
    /// <param name="mechanism">The mechanism to remove.</param>
    /// <param name="coordinates">The coordinates to drop it at.</param>
    /// <param name="part">Resolve comp</param>
    /// <returns>True if it was removed, false otherwise.</returns>
    public bool TryRemoveMechanism(EntityUid uid, MechanismComponent mechanism, EntityCoordinates coordinates,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (TryRemoveMechanism(uid, mechanism, part))
        {
            Transform(mechanism.Owner).Coordinates = coordinates;
            return true;
        }

        return false;
    }

    protected bool RemoveMechanismAndRaiseEvents(MechanismComponent mechanism, SharedBodyPartComponent part)
    {
        if (part.MechanismContainer == null)
            return false;

        if (!part.MechanismContainer.Remove(mechanism.Owner))
            return false;

        mechanism.Part = null;

        if (part.Body == null)
            RaiseLocalEvent(mechanism.Owner, new MechanismRemovedFromPartEvent(part.Owner));
        else
            RaiseLocalEvent(mechanism.Owner, new MechanismRemovedFromPartInBodyEvent(part.Body.Owner, part.Owner));

        return true;
    }

    /// <summary>
    ///     Tries to destroy the given <see cref="MechanismComponent" /> from
    ///     this part.
    ///     The mechanism won't be deleted if it is not in this body part.
    /// </summary>
    /// <returns>
    ///     True if the mechanism was in this body part and destroyed,
    ///     false otherwise.
    /// </returns>
    public bool DeleteMechanism(EntityUid uid, MechanismComponent mechanism,
        SharedBodyPartComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        if (!TryRemoveMechanism(uid, mechanism, body)) return false;

        QueueDel(mechanism.Owner);
        return true;
    }
}
