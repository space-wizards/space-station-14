using System.Diagnostics.CodeAnalysis;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed partial class EntityWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<ItemComponent> _itemQuery;
    private string _itemComponentName = string.Empty;
    private string _tagComponentName = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        _itemQuery = GetEntityQuery<ItemComponent>();

        // caching for minor performance improvement
        _itemComponentName = Factory.GetComponentName<ItemComponent>();
        _tagComponentName = Factory.GetComponentName<TagComponent>();
    }

    /// <summary>
    /// Checks whether a given entity satisfies a whitelist.
    /// Returns false if the entity is null.
    /// </summary>
    public bool IsValid(EntityWhitelist list, [NotNullWhen(true)] EntityUid? uid)
    {
        return uid != null && IsValid(list, uid.Value);
    }

    /// <summary>
    /// Checks whether a given entity satisfies a whitelist.
    /// </summary>
    public bool IsValid(EntityWhitelist list, EntityUid uid)
    {
        if (list.Components != null)
        {
            if (list.Registrations == null)
            {
                var regs = StringsToRegs(list.Components);
                list.Registrations = new List<ComponentRegistration>();
                list.Registrations.AddRange(regs);
            }
        }

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

    /// The following are a list of "helper functions" that are basically the same as each other
    /// to help make code that uses EntityWhitelist a bit more readable because at the moment
    /// it is quite clunky having to write out component.Whitelist == null ? true : _whitelist.IsValid(component.Whitelist, uid)
    /// several times in a row and makes comparisons easier to read

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
    /// Helper function to determine if a whitelist is not null and the entity is on list.
    /// </summary>
    public bool IsWhitelistPass(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is not null and the entity is not on the list.
    /// </summary>
    public bool IsWhitelistFail(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return false;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is either null or the entity is on the list.
    /// </summary>
    public bool IsWhitelistPassOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if a whitelist is either null or the entity is not on the list.
    /// </summary>
    public bool IsWhitelistFailOrNull(EntityWhitelist? whitelist, EntityUid uid)
    {
        if (whitelist == null)
            return true;

        return !IsValid(whitelist, uid);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is not null and the entity is on list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistPass(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPass(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is not null and the entity is not on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistFail(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFail(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is either null or the entity is on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistPassOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistPassOrNull(blacklist, uid);
    }

    /// <summary>
    /// Helper function to determine if a blacklist is either null or the entity is not on the list.
    /// Duplicate of equivalent whitelist function.
    /// </summary>
    public bool IsBlacklistFailOrNull(EntityWhitelist? blacklist, EntityUid uid)
    {
        return IsWhitelistFailOrNull(blacklist, uid);
    }

    private List<ComponentRegistration> StringsToRegs(string[]? input)
    {
        var list = new List<ComponentRegistration>();

        if (input == null || input.Length == 0)
            return list;

        foreach (var name in input)
        {
            var availability = Factory.GetComponentAvailability(name);
            if (Factory.TryGetRegistration(name, out var registration)
                && availability == ComponentAvailability.Available)
            {
                list.Add(registration);
            }
            else if (availability == ComponentAvailability.Unknown)
            {
                Log.Error($"StringsToRegs failed: Unknown component name {name} passed to EntityWhitelist!");
            }
        }

        return list;
    }
}
