using System.Diagnostics.CodeAnalysis;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed class EntityWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<ItemComponent> _itemQuery;

    public override void Initialize()
    {
        base.Initialize();
        _itemQuery = GetEntityQuery<ItemComponent>();
    }

    /// <inheritdoc cref="IsValid(Content.Shared.Whitelist.EntityWhitelist,Robust.Shared.GameObjects.EntityUid)"/>
    public bool IsValid(EntityWhitelist list, [NotNullWhen(true)] EntityUid? uid)
    {
        return uid != null && IsValid(list, uid.Value);
    }

    /// <summary>
    /// Checks whether a given entity is allowed by a whitelist and not blocked by a blacklist.
    /// If a blacklist is provided and it matches then this returns false.
    /// If a whitelist is provided and it does not match then this returns false.
    /// If either list is null it does not get checked.
    /// </summary>
    public bool CheckBoth([NotNullWhen(true)] EntityUid? uid, EntityWhitelist? blacklist = null, EntityWhitelist? whitelist = null)
    {
        if (uid == null)
            return false;

        if (blacklist != null && IsValid(blacklist, uid))
            return false;

        return whitelist == null || IsValid(whitelist, uid);
    }

    /// <summary>
    /// Checks whether a given entity satisfies a whitelist.
    /// </summary>
    public bool IsValid(EntityWhitelist list, EntityUid uid)
    {
        if (list.Components != null)
            EnsureRegistrations(list);

        if (list.Registrations != null)
        {
            foreach (var reg in list.Registrations)
            {
                if (HasComp(uid, reg.Type))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null && _itemQuery.TryComp(uid, out var itemComp))
        {
            if (list.Sizes.Contains(itemComp.Size))
                return true;
        }

        if (list.Tags != null)
        {
            return list.RequireAll
                ? _tag.HasAllTags(uid, list.Tags)
                : _tag.HasAnyTag(uid, list.Tags);
        }

        return list.RequireAll;
    }
        /// <summary>
    /// Checks if the prototype is valid.
    /// </summary>
    /// <param name="list">The list to check</param>
    /// <param name="prototype">the prototype to check</param>
    /// <returns>True if it is valid</returns>
    public bool IsPrototypeValid(EntityWhitelist list, EntityPrototype prototype)
    {
        if (list.Components != null)
            EnsureRegistrations(list);

        if (list.Registrations != null)
        {
            foreach (var reg in list.Registrations)
            {
                if (prototype.Components.ContainsKey(reg.Type.ToString()))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        if (list.Sizes != null && prototype.Components.TryGetComponent("Item", out var itemComp) && itemComp is ItemComponent component)
        {
            if (list.Sizes.Contains(component.Size))
                return true;
        }

        if (list.Tags != null)
        {
            if (prototype.Components.TryGetComponent("Tag", out var tagComponent) &&
                tagComponent is TagComponent comp)
            {
                return list.RequireAll
                    ? _tag.HasAllTags(comp, list.Tags)
                    : _tag.HasAnyTag(comp, list.Tags);
            }

        }

        return list.RequireAll;
    }

    /// The following are a list of "helper functions" that are basically the same as each other
    /// to help make code that uses EntityWhitelist a bit more readable because at the moment
    /// it is quite clunky having to write out component.Whitelist == null ? true : _whitelist.IsValid(component.Whitelist, uid)
    /// several times in a row and makes comparisons easier to read

    /// <summary>
    /// Checks if a given EntityPrototype passes the given whitelist
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns>True if it passes the whitelist</returns>
    public bool IsPrototypeWhitelistPass(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist != null && IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// Checks if a given EntityPrototype passes the given whitelist
    /// </summary>
    /// <param name="whitelist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns>True if it fails the whitelist</returns>
    public bool IsPrototypeWhitelistFail(EntityWhitelist? whitelist, EntityPrototype prototype)
    {
        return whitelist != null && !IsPrototypeValid(whitelist, prototype);
    }

    /// <summary>
    /// Checks if a given EntityPrototype passes the given blacklist
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns>True if it passes the blacklist</returns>
    public bool IsPrototypeBlacklistPass(EntityWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistPass(blacklist, prototype);
    }

    /// <summary>
    /// Checks if a given EntityPrototype fails the given blacklist
    /// </summary>
    /// <param name="blacklist">The whitelist to check</param>
    /// <param name="prototype">The prototype to check</param>
    /// <returns>True if it fails the blacklist</returns>
    public bool IsPrototypeBlacklistFail(EntityWhitelist? blacklist, EntityPrototype prototype)
    {
        return IsPrototypeWhitelistFail(blacklist, prototype);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is on list
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is not null and entity is not on the list
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is on the list
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Whitelist is either null or the entity is not on the list
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is on list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPass(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPass(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is not null and entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFail(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFail(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistPassOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPassOrNull(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if Blacklist is either null or the entity is not on the list
    /// Duplicate of equivalent Whitelist function
    /// </summary>
    public bool IsBlacklistFailOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFailOrNull(blacklist, uid);
    }

    private void EnsureRegistrations(EntityWhitelist list)
    {
        if (list.Components == null)
            return;

        list.Registrations = new List<ComponentRegistration>();
        foreach (var name in list.Components)
        {
            var availability = _factory.GetComponentAvailability(name);
            if (_factory.TryGetRegistration(name, out var registration)
                && availability == ComponentAvailability.Available)
            {
                list.Registrations.Add(registration);
            }
            else if (availability == ComponentAvailability.Unknown)
            {
                Log.Warning($"Unknown component name {name} passed to EntityWhitelist!");
            }
        }
    }
}
