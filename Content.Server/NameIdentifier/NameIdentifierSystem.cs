using Content.Shared.GameTicking;
using Content.Shared.NameIdentifier;
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

    [ViewVariables]
    public Dictionary<NameIdentifierGroupPrototype, HashSet<int>> CurrentIds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NameIdentifierComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(CleanupIds);

        InitialSetupPrototypes();
        _prototypeManager.PrototypesReloaded += OnReloadPrototypes;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _prototypeManager.PrototypesReloaded -= OnReloadPrototypes;
    }

    /// <summary>
    ///     Generates a new unique name/suffix for a given entity and adds it to <see cref="CurrentIds"/>
    ///     but does not set the entity's name.
    /// </summary>
    public string GenerateUniqueName(EntityUid uid, NameIdentifierGroupPrototype proto)
    {
        var entityName = Name(uid);
        if (!CurrentIds.TryGetValue(proto, out var set))
            return entityName;

        if (set.Count == (proto.MaxValue - proto.MinValue) + 1)
        {
            // Oh jeez. We're outta numbers.
            return entityName;
        }

        // This is kind of inefficient with very large amounts of entities but its better than any other method
        // I could come up with.

        var randomVal = _robustRandom.Next(proto.MinValue, proto.MaxValue);
        while (set.Contains(randomVal))
        {
            randomVal = _robustRandom.Next(proto.MinValue, proto.MaxValue);
        }

        set.Add(randomVal);

        return proto.Prefix is not null
            ? $"{proto.Prefix}-{randomVal}"
            : $"{randomVal}";
    }

    private void OnComponentInit(EntityUid uid, NameIdentifierComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex<NameIdentifierGroupPrototype>(component.Group, out var group))
            return;

        // Generate a new name.
        var meta = MetaData(uid);
        var uniqueName = GenerateUniqueName(uid, group);

        // "DR-1234" as opposed to "drone (DR-1234)"
        meta.EntityName = group.FullName
            ? uniqueName
            : $"{meta.EntityName} ({uniqueName})";
    }

    private void InitialSetupPrototypes()
    {
        foreach (var proto in _prototypeManager.EnumeratePrototypes<NameIdentifierGroupPrototype>())
        {
            CurrentIds.Add(proto, new());
        }
    }

    private void OnReloadPrototypes(PrototypesReloadedEventArgs ev)
    {
        if (!ev.ByType.TryGetValue(typeof(NameIdentifierGroupPrototype), out var set))
            return;

        foreach (var (_, proto) in set.Modified)
        {
            if (proto is not NameIdentifierGroupPrototype group)
                continue;

            // Only bother adding new ones.
            if (CurrentIds.ContainsKey(group))
                continue;

            CurrentIds.Add(group, new());
        }
    }

    private void CleanupIds(RoundRestartCleanupEvent ev)
    {
        foreach (var (_, set) in CurrentIds)
        {
            set.Clear();
        }
    }
}
