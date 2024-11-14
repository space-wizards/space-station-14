using System.Diagnostics.CodeAnalysis;
using Content.Shared.Item;
using Content.Shared.Roles;
using Content.Shared.Tag;

namespace Content.Shared.Whitelist;

public sealed class EntityWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<ItemComponent> _itemQuery;
    private readonly Dictionary<string, (ComponentAvailability availability, ComponentRegistration? registration)> _componentCache = new();

    public override void Initialize()
    {
        base.Initialize();
        _itemQuery = GetEntityQuery<ItemComponent>();

        foreach (var registration in _factory.GetAllRegistrations())
        {
            _componentCache[registration.Name] = (ComponentAvailability.Available, registration);
        }
    }

    /// <summary>
    /// Converts strings to registrations once and caches the result
    /// </summary>
    private void EnsureRegistrations(EntityWhitelist list)
    {
        // If we've already converted the Components, no need to do it again
        if (list.Registrations != null)
            return;

        // Initialize registrations list
        var capacity = (list.Components?.Length ?? 0) + (list.MindRoles?.Length ?? 0);
        list.Registrations = new List<ComponentRegistration>(capacity);

        // Process both component lists
        ProcessNames(list.Components);
        ProcessNames(list.MindRoles);
        return;

        void ProcessNames(string[]? names)
        {
            if (names == null)
                return;

            foreach (var name in names)
            {
                // Skip if we already know it's unknown (cached with null registration)
                if (_componentCache.TryGetValue(name, out var cached))
                {
                    if (cached.registration != null && !list.Registrations.Contains(cached.registration))
                    {
                        list.Registrations.Add(cached.registration);
                    }
                    continue;
                }

                // First time seeing this component name
                var availability = _factory.GetComponentAvailability(name);
                ComponentRegistration? registration = null;

                if (availability == ComponentAvailability.Available)
                {
                    _factory.TryGetRegistration(name, out registration);
                }

                else if (availability == ComponentAvailability.Unknown)
                {
                    // Only log unknown components once when we first see them
                    Log.Error($"Unknown component name {name} passed to EntityWhitelist!");
                }

                // Cache the result (including nulls for unknown components)
                _componentCache[name] = (availability, registration);

                if (registration != null && !list.Registrations.Contains(registration))
                {
                    list.Registrations.Add(registration);
                }
            }
        }
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
        // Fast path for empty lists
        if ((list.Components == null || list.Components.Length == 0) &&
            (list.MindRoles == null || list.MindRoles.Length == 0) &&
            (list.Tags == null || list.Tags.Count == 0) &&
            (list.Registrations == null || list.Registrations.Count == 0) &&
            list.Sizes == null)
        {
            return list.RequireAll;
        }

        // Convert Components and MindRoles to Registrations
        EnsureRegistrations(list);

        // Check mind roles first
        if (list.MindRoles is { Length: > 0 })
        {
            foreach (var roleName in list.MindRoles)
            {
                if (!_componentCache.TryGetValue(roleName, out var cached) || cached.registration == null)
                    continue;

                if (_roles.MindHasRole(uid, cached.registration.Type, out _))
                {
                    if (!list.RequireAll)
                        return true;
                }
                else if (list.RequireAll)
                    return false;
            }
        }

        // Check registrations
        if (list.Registrations != null && list.Registrations.Count > 0)
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

        // Check sizes
        if (list.Sizes != null && _itemQuery.TryComp(uid, out var itemComp))
        {
            if (list.Sizes.Contains(itemComp.Size))
            {
                if (!list.RequireAll)
                    return true;
            }
            else if (list.RequireAll)
                return false;
        }

        // Check tags
        if (list.Tags == null)
            return list.RequireAll;

        var hasTag = list.RequireAll
            ? _tag.HasAllTags(uid, list.Tags)
            : _tag.HasAnyTag(uid, list.Tags);

        if (!list.RequireAll && hasTag)
            return true;

        if (list.RequireAll && !hasTag)
            return false;

        return list.RequireAll;
    }

    /// The following are a list of "helper functions" that are basically the same as each other
    /// to help make code that uses EntityWhitelist a bit more readable because at the moment
    /// it is quite clunky having to write out component.Whitelist == null ? true : _whitelist.IsValid(component.Whitelist, uid)
    /// several times in a row and makes comparisons easier to read

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
}
