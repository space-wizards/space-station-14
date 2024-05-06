using System.Diagnostics.CodeAnalysis;
using Content.Shared.Item;
using Content.Shared.Tag;

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
