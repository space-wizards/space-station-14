using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBodySystem
{
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

        foreach (var (part, _) in body.Parts)
        foreach (var mechanism in part.Mechanisms)
        {
            if (EntityManager.TryGetComponent<T>((mechanism).Owner, out var comp))
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

    #region Slots

    // TODO BODY optimize this
    public BodyPartSlot SlotAt(SharedBodyComponent body, int index)
    {
        return body.SlotIds.Values.ElementAt(index);
    }

    public BodyPartSlot? GetSlot(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return null;
        return body.Parts.GetValueOrDefault(part);
    }

    public bool TryGetSlot(EntityUid uid, SharedBodyPartComponent part, [NotNullWhen(true)] out BodyPartSlot? slot,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
        {
            slot = null;
            return false;
        }
        return (slot = GetSlot(uid, part, body)) != null;
    }

    public BodyPartSlot? GetCenterSlot(EntityUid uid,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return null;

        var template = _prototypeManager.Index<BodyTemplatePrototype>(body.TemplateId);
        return body.SlotIds.GetValueOrDefault(template.CenterSlot);
    }

    public IEnumerable<BodyPartSlot> GetSlotsOfType(EntityUid uid, BodyPartType type,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var slot in body.SlotIds.Values)
        {
            if (slot.PartType == type)
            {
                yield return slot;
            }
        }
    }

    #endregion

    #region Parts

    public SharedBodyPartComponent? GetCenterPart(EntityUid uid,
        SharedBodyComponent? body = null)
    {
        if (!Resolve(uid, ref body))
            return null;

        return GetCenterSlot(uid, body)?.Part;
    }

    public IEnumerable<SharedBodyPartComponent> GetPartsOfType(EntityUid uid, BodyPartType type,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            yield break;

        foreach (var slot in GetSlotsOfType(uid, type, body))
        {
            if (slot.Part != null)
            {
                yield return slot.Part;
            }
        }
    }

    public Dictionary<BodyPartSlot, SharedBodyPartComponent> GetHangingParts(EntityUid uid, BodyPartSlot from,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body))
            return new();

        var hanging = new Dictionary<BodyPartSlot, SharedBodyPartComponent>();

        foreach (var connection in from.Connections)
        {
            if (connection.Part != null &&
                !ConnectedToCenter(uid, connection.Part, body))
            {
                hanging.Add(connection, connection.Part);
            }
        }

        return hanging;
    }

    public bool HasPart(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
    {
        if (!Resolve(uid, ref body, false))
            return false;

        return body.Parts.ContainsKey(part);
    }

    public bool ConnectedToCenter(EntityUid uid, SharedBodyPartComponent part,
        SharedBodyComponent? body=null)
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
        if (slot.Part == centerPart)
            return true;

        // recursive case
        searched.Add(slot);

        foreach (var connection in slot.Connections)
        {
            if (!searched.Contains(connection) &&
                ConnectedToCenterPartRecursion(uid, connection, body, searched))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

}
