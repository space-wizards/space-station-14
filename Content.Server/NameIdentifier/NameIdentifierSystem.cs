using Content.Shared.GameTicking;
using Content.Shared.NameIdentifier;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.NameIdentifier;

/// <summary>
///     Handles unique name identifiers for entities e.g. `monkey (MK-912)`
/// </summary>
public sealed class NameIdentifierSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    /// <summary>
    /// Free IDs available per <see cref="NameIdentifierGroupPrototype"/>.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, List<int>> CurrentIds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NameIdentifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NameIdentifierComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(CleanupIds);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnReloadPrototypes);

        InitialSetupPrototypes();
    }

    private void OnComponentShutdown(EntityUid uid, NameIdentifierComponent component, ComponentShutdown args)
    {
        if (CurrentIds.TryGetValue(component.Group, out var ids))
        {
            // Avoid inserting the value right back at the end or shuffling in place:
            // just pick a random spot to put it and then move that one to the end.
            var randomIndex = _robustRandom.Next(ids.Count);
            var random = ids[randomIndex];
            ids[randomIndex] = component.Identifier;
            ids.Add(random);
        }
    }

    /// <summary>
    ///     Generates a new unique name/suffix for a given entity and adds it to <see cref="CurrentIds"/>
    ///     but does not set the entity's name.
    /// </summary>
    public string GenerateUniqueName(EntityUid uid, ProtoId<NameIdentifierGroupPrototype> proto, out int randomVal)
    {
        return GenerateUniqueName(uid, _prototypeManager.Index(proto), out randomVal);
    }

    /// <summary>
    ///     Generates a new unique name/suffix for a given entity and adds it to <see cref="CurrentIds"/>
    ///     but does not set the entity's name.
    /// </summary>
    public string GenerateUniqueName(EntityUid uid, NameIdentifierGroupPrototype proto, out int randomVal)
    {
        randomVal = 0;
        var entityName = Name(uid);
        if (!CurrentIds.TryGetValue(proto.ID, out var set))
            return entityName;

        if (set.Count == 0)
        {
            // Oh jeez. We're outta numbers.
            return entityName;
        }

        randomVal = set[^1];
        set.RemoveAt(set.Count - 1);

        return proto.Prefix is not null
            ? $"{proto.Prefix}-{randomVal}"
            : $"{randomVal}";
    }

    private void OnMapInit(EntityUid uid, NameIdentifierComponent component, MapInitEvent args)
    {
        if (!_prototypeManager.TryIndex<NameIdentifierGroupPrototype>(component.Group, out var group))
            return;

        int id;
        string uniqueName;

        // If it has an existing valid identifier then use that, otherwise generate a new one.
        if (component.Identifier != -1 &&
            CurrentIds.TryGetValue(component.Group, out var ids) &&
            ids.Remove(component.Identifier))
        {
            id = component.Identifier;
            uniqueName = group.Prefix is not null
                ? $"{group.Prefix}-{id}"
                : $"{id}";
        }
        else
        {
            uniqueName = GenerateUniqueName(uid, group, out id);
            component.Identifier = id;
        }

        component.FullIdentifier = group.FullName
            ? uniqueName
            : $"({uniqueName})";

        var meta = MetaData(uid);
        // "DR-1234" as opposed to "drone (DR-1234)"
        _metaData.SetEntityName(uid, group.FullName
            ? uniqueName
            : $"{meta.EntityName} ({uniqueName})", meta);
        Dirty(uid, component);
    }

    private void InitialSetupPrototypes()
    {
        EnsureIds();
    }

    private void FillGroup(NameIdentifierGroupPrototype proto, List<int> values)
    {
        values.Clear();
        for (var i = proto.MinValue; i < proto.MaxValue; i++)
        {
            values.Add(i);
        }

        _robustRandom.Shuffle(values);
    }

    private List<int> GetOrCreateIdList(NameIdentifierGroupPrototype proto)
    {
        if (!CurrentIds.TryGetValue(proto.ID, out var ids))
        {
            ids = new List<int>(proto.MaxValue - proto.MinValue);
            CurrentIds.Add(proto.ID, ids);
        }

        return ids;
    }

    private void EnsureIds()
    {
        foreach (var proto in _prototypeManager.EnumeratePrototypes<NameIdentifierGroupPrototype>())
        {
            var ids = GetOrCreateIdList(proto);

            FillGroup(proto, ids);
        }
    }

    private void OnReloadPrototypes(PrototypesReloadedEventArgs ev)
    {
        if (!ev.ByType.TryGetValue(typeof(NameIdentifierGroupPrototype), out var set))
            return;

        var toRemove = new ValueList<string>();

        foreach (var proto in CurrentIds.Keys)
        {
            if (!_prototypeManager.HasIndex<NameIdentifierGroupPrototype>(proto))
            {
                toRemove.Add(proto);
            }
        }

        foreach (var proto in toRemove)
        {
            CurrentIds.Remove(proto);
        }

        foreach (var proto in set.Modified.Values)
        {
            var name_proto = (NameIdentifierGroupPrototype) proto;

            // Only bother adding new ones.
            if (CurrentIds.ContainsKey(proto.ID))
                continue;

            var ids  = GetOrCreateIdList(name_proto);
            FillGroup(name_proto, ids);
        }
    }


    private void CleanupIds(RoundRestartCleanupEvent ev)
    {
        EnsureIds();
    }
}
