using System.Drawing;
using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Systems.Part;

/// <summary>
///     Contains utility functions for getting information about a body part.
/// </summary>
public abstract partial class SharedBodyPartSystem
{
    // TODO maybe cache. idk doesnt really matter
    /// <summary>
    ///     Returns the current size of this part, based on
    ///     the added up size of its mechanisms.
    /// </summary>
    public int GetCurrentSize(EntityUid uid,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return 0;

        int size = 0;
        foreach (var mech in part.Mechanisms)
        {
            size += mech.Size;
        }

        return size;
    }

    public bool CanAddMechanism(EntityUid uid, MechanismComponent mechanism,
        SharedBodyPartComponent? part = null)
    {
        if (!Resolve(uid, ref part))
            return false;

        return GetCurrentSize(uid, part) + mechanism.Size <= part.Size;
    }
}
