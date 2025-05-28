using System.Diagnostics.CodeAnalysis;
using Content.Shared.Item;
using Content.Shared.Tag;

namespace Content.Shared.Whitelist;

public partial class EntityWhitelistSystem : EntitySystem
{
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
