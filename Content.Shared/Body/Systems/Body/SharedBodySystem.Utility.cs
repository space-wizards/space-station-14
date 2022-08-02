using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Shared.Body.Systems.Body;

public abstract partial class SharedBodySystem
{
    #region Mechanisms

    /// <summary>
    ///     Returns a list of ValueTuples of <see cref="T"/> and MechanismComponent on each mechanism
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="body">The body to check for mechanisms on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public IEnumerable<(T Comp, MechanismComponent Mech)> GetComponentsOnMechanisms<T>(EntityUid uid,
        SharedBodyComponent? body = null) where T : Component
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var part in GetAllParts(uid, body))
            foreach (var mechanism in BodyPartSystem.GetAllMechanisms(part.Owner, part))
            {
                if (TryComp<T>(mechanism.Owner, out var comp))
                    yield return (comp, mechanism);
            }
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and MechanismComponent on each mechanism
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="body">The body to check for mechanisms on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetComponentsOnMechanisms<T>(EntityUid uid,
        [NotNullWhen(true)] out IEnumerable<(T Comp, MechanismComponent Mech)>? comps,
        SharedBodyComponent? body = null) where T : Component
    {
        if (!Resolve(uid, ref body))
        {
            comps = null;
            return false;
        }

        comps = GetComponentsOnMechanisms<T>(uid, body).ToArray();

        if (!comps.Any())
        {
            comps = null;
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns a list of ValueTuples of <see cref="T"/> and SharedBodyPartComponent on each body part
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="body">The body to check for parts on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    public IEnumerable<(T Comp, SharedBodyPartComponent Part)> GetComponentsOnParts<T>(EntityUid uid, SharedBodyComponent? body = null)
        where T : Component
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var part in GetAllParts(uid, body))
        {
            if (TryComp<T>(part.Owner, out var comp))
                yield return (comp, part);
        }
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and SharedBodyPartComponent on each body part
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The entity to check for the component on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="body">The body to check for parts on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetComponentsOnParts<T>(EntityUid uid,
        [NotNullWhen(true)] out IEnumerable<(T Comp, SharedBodyPartComponent Part)>? comps,
        SharedBodyComponent? body = null) where T : Component
    {
        if (!Resolve(uid, ref body))
        {
            comps = null;
            return false;
        }

        comps = GetComponentsOnParts<T>(uid, body).ToArray();

        if (!comps.Any())
        {
            comps = null;
            return false;
        }

        return true;
    }

    #endregion

    #region Slots

    /// <summary>
    ///     Gets the <see cref="BodyPartSlot"/> of the provided body part from the body
    /// </summary>
    public BodyPartSlot? GetSlot(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return null;

        foreach(var (_, slot) in body.Slots)
        {
            if (slot.HasPart && slot.Part == part.Owner)
                return slot;
        }

        return null;
    }

    /// <summary>
    ///     Tries to get the <see cref="BodyPartSlot"/> of the provided part from the body
    /// </summary>
    public bool TryGetSlot(EntityUid uid, SharedBodyPartComponent part, [NotNullWhen(true)] out BodyPartSlot? slot,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
        {
            slot = null;
            return false;
        }
        return (slot = GetSlot(uid, part, body)) != null;
    }

    /// <summary>
    ///     Each body template can have a 'center' slot, usually a torso. This gets that.
    /// </summary>
    public BodyPartSlot? GetCenterSlot(EntityUid uid,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return null;

        foreach (var slot in body.Slots.Values)
        {
            if (slot.IsCenterSlot)
                return slot;
        }

        return null;
    }

    /// <summary>
    ///     Returns all slots with a given part type.
    /// </summary>
    /// <remarks>
    ///     This includes slots that do not have a part inserted,
    ///     so it can be used for things like checking how many parts an entity 'should' have.
    /// </remarks>
    public IEnumerable<BodyPartSlot> GetSlotsOfType(EntityUid uid, BodyPartType type,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var slot in body.Slots.Values)
        {
            if (slot.PartType == type)
            {
                yield return slot;
            }
        }
    }

    private static string GenerateUniqueSlotName(SharedBodyPartComponent part)
    {
        // e.g. 8912-Arm-Left
        // Can't see how we'd get a collision from this.
        return $"{part.Owner}-{part.PartType}-{part.Compatibility}";
    }

    #endregion

    #region Parts

    /// <summary>
    ///     Returns all parts on the body
    /// </summary>
    public IEnumerable<SharedBodyPartComponent> GetAllParts(EntityUid uid, SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var (_, slot) in body.Slots)
        {
            if (slot.HasPart && TryComp<SharedBodyPartComponent>(slot?.Part, out var part))
                yield return part;
        }
    }

    /// <summary>
    /// Gets the part in the center slot of the body, if any
    /// </summary>
    public SharedBodyPartComponent? GetCenterPart(EntityUid uid,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return null;

        var partId = GetCenterSlot(uid, body)?.Part;

        TryComp<SharedBodyPartComponent>(partId, out var part);
        return part;
    }

    public bool HasPartOfType(EntityUid uid, BodyPartType type,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, false))
            return false;

        return GetPartsOfType(uid, type).Any();
    }

    /// <summary>
    ///     Returns all parts of a given type.
    /// </summary>
    public IEnumerable<SharedBodyPartComponent> GetPartsOfType(EntityUid uid, BodyPartType type,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var part in GetAllParts(uid, body))
        {
            if (part.PartType == type)
                yield return part;
        }
    }

    /// <summary>
    ///     Gets all slots/parts that are "hanging" from the part in the provided slot, meaning they are
    ///     connected to the provided part but have no connection to the center body part otherwise
    /// </summary>
    public Dictionary<BodyPartSlot, SharedBodyPartComponent> GetHangingParts(EntityUid uid, BodyPartSlot from,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return new();

        var hanging = new Dictionary<BodyPartSlot, SharedBodyPartComponent>();

        foreach (var connection in ResolveBodyPartSlotsById(from.Connections, body))
        {
            if (connection.Part != null && TryComp<SharedBodyPartComponent>(connection.Part, out var part) &&
                !ConnectedToCenter(uid, part, body))
            {
                hanging.Add(connection, part);
            }
        }

        return hanging;
    }

    /// <summary>
    ///     Checks if the provided part is already in any of the body's slots
    /// </summary>
    public bool HasPart(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, false))
            return false;

        return body.Slots.Values.Any(_ => _.Part != null && _.Part == part.Owner);
    }

    /// <summary>
    ///     Checks if the part is compatible with any of the parts it would connect to if added to the provided slot.
    /// </summary>
    public bool IsCompatible(EntityUid uid, SharedBodyPartComponent part, BodyPartSlot slot, SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        // Part is universal so we can skip all other checks
        if (part.Compatibility == null)
            return true;

        foreach (var connection in ResolveBodyPartSlotsById(slot.Connections, body))
        {
            if (!connection.HasPart)
                continue;

            if (!TryComp<SharedBodyPartComponent>(uid, out var connectedPart))
                continue;

            if (connectedPart.Compatibility != part.Compatibility)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checcks if the provided part is connected to the center body part of the body
    /// </summary>
    public bool ConnectedToCenter(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return false;

        return TryGetSlot(uid, part, out var result, body) &&
               ConnectedToCenterPartRecursion(uid, result, body);
    }

    private bool ConnectedToCenterPartRecursion(EntityUid uid, BodyPartSlot slot, SharedBodyComponent body, HashSet<BodyPartSlot>? searched = null)
    {
        searched ??= new HashSet<BodyPartSlot>();

        var centerPart = GetCenterPart(uid, body);

        if (centerPart == null)
            return false;

        // base case
        if (slot.Part == centerPart.Owner)
            return true;

        // recursive case
        searched.Add(slot);

        foreach (var connection in ResolveBodyPartSlotsById(slot.Connections, body))
        {
            if (!searched.Contains(connection) &&
                ConnectedToCenterPartRecursion(uid, connection, body, searched))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<BodyPartSlot> ResolveBodyPartSlotsById(HashSet<string> slotIds, SharedBodyComponent body)
    {
        foreach (var slotId in slotIds)
        {
            if (body.Slots.TryGetValue(slotId, out var slot))
                yield return slot;
        }
    }

    #endregion
}
