using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.Body.Systems.Part;

public abstract partial class SharedBodyPartSystem
{
    // TODO maybe clean up the try stuff

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

        if (!force && !CanAddMechanism(uid, mechanism, part)) return false;

        if (!part.Mechanisms.Add(mechanism)) return false;

        RaiseMechanismEvent(mechanism.Owner, part, mechanism);

        mechanism.Part = part;
        part.Dirty();

        OnAddMechanism(uid, mechanism, part);

        return true;
    }

    /// <summary>
    ///     Tries to remove the given <see cref="mechanism" /> from this part.
    /// </summary>
    /// <param name="uid">The part to remove a mechanism from</param>
    /// <param name="mechanism">The mechanism to remove.</param>
    /// <param name="part"></param>
    /// <returns>True if it was removed, false otherwise.</returns>
    public bool RemoveMechanism(EntityUid uid, MechanismComponent mechanism,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (!part.Mechanisms.Remove(mechanism)) return false;
        mechanism.Part = null;

        RaiseMechanismEvent(mechanism.Owner, part, mechanism);

        part.Dirty();

        OnRemoveMechanism(uid, mechanism, part);

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
    public bool RemoveMechanism(EntityUid uid, MechanismComponent mechanism, EntityCoordinates coordinates,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        if (RemoveMechanism(uid, mechanism, part))
        {
            Transform(mechanism.Owner).Coordinates = coordinates;
            return true;
        }

        return false;
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

        if (!RemoveMechanism(uid, mechanism, body)) return false;

        QueueDel(mechanism.Owner);
        return true;
    }

    /// <summary>
    ///     Finds out which mechanism event to raise depending on the circumstances.
    /// </summary>
    /// <param name="uid">The mechanism's entity.</param>
    /// <param name="part">The part to set the mechanism's owner to. Can be null.</param>
    /// <param name="mech">Resolve comp</param>
    public void RaiseMechanismEvent(EntityUid uid, SharedBodyPartComponent? part,
        MechanismComponent? mech = null)
    {
        if (!Resolve(uid, ref mech))
            return;

        if (part == mech.Part)
            return;

        // We're replacing an existing part
        if (mech.Part != null)
        {
            // It's on a body
            if (mech.Part.Body != null)
            {
                RaiseLocalEvent(uid, new MechanismRemovedFromPartInBodyEvent(mech.Part.Body.Owner, mech.Part.Owner), true);
            }
            else
            {
                RaiseLocalEvent(uid, new MechanismRemovedFromPartEvent(mech.Part.Owner), true);
            }
        }

        if (part != null)
        {
            if (part.Body != null)
            {
                RaiseLocalEvent(uid, new MechanismAddedToPartInBodyEvent(part.Body.Owner, part.Owner), true);
            }
            else
            {
                RaiseLocalEvent(uid, new MechanismAddedToPartEvent(part.Owner), true);
            }
        }
    }

    /// <summary>
    ///     Gibs the body part.
    /// </summary>
    public void GibPart(EntityUid uid,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return;

        foreach (var mechanism in part.Mechanisms)
        {
            RemoveMechanism(uid, mechanism, part);
        }
    }

    /// <remarks>
    ///     These are needed so that the server can
    ///     insert/remove parts from their container.
    /// </remarks>
    protected virtual void OnAddMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent part)
    {
    }

    protected virtual void OnRemoveMechanism(EntityUid uid, MechanismComponent mechanism, SharedBodyPartComponent part)
    {
    }
}
