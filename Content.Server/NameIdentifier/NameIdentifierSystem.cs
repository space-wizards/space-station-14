using Content.Shared.GameTicking;
using Content.Shared.NameIdentifier;
using Content.Shared.NameModifier.EntitySystems;
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
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    /// <summary>
    /// Free IDs available per <see cref="NameIdentifierGroupPrototype"/>.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, List<int>> CurrentIds = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NameIdentifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NameIdentifierComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<NameIdentifierComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(CleanupIds);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnReloadPrototypes);

        InitialSetupPrototypes();
    }

    private void OnComponentShutdown(Entity<NameIdentifierComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Group is null)
            return;

        if (CurrentIds.TryGetValue(ent.Comp.Group, out var ids) && ids.Count > 0)
        {
            // Avoid inserting the value right back at the end or shuffling in place:
            // just pick a random spot to put it and then move that one to the end.
            var randomIndex = _robustRandom.Next(ids.Count);
            var random = ids[randomIndex];
            ids[randomIndex] = ent.Comp.Identifier;
            ids.Add(random);
        }

        _nameModifier.RefreshNameModifiers(ent.Owner);
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

        return proto.Format is not null
            ? Loc.GetString(proto.Format, ("number", randomVal))
            : $"{randomVal}";
    }

    private void OnMapInit(Entity<NameIdentifierComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Group is null)
            return;

        if (!_prototypeManager.Resolve(ent.Comp.Group, out var group))
            return;

        int id;
        string uniqueName;

        // If it has an existing valid identifier then use that, otherwise generate a new one.
        if (ent.Comp.Identifier != -1 &&
            CurrentIds.TryGetValue(ent.Comp.Group, out var ids) &&
            ids.Remove(ent.Comp.Identifier))
        {
            id = ent.Comp.Identifier;
            uniqueName = group.Format is not null
                ? Loc.GetString(group.Format, ("number", id))
                : $"{id}";
        }
        else
        {
            uniqueName = GenerateUniqueName(ent, group, out id);
            ent.Comp.Identifier = id;
        }

        ent.Comp.FullIdentifier = group.FullName
            ? uniqueName
            : $"({uniqueName})";

        Dirty(ent);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnRefreshNameModifiers(Entity<NameIdentifierComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (ent.Comp.Group is null)
            return;

        // Don't apply the modifier if the component is being removed
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!_prototypeManager.Resolve(ent.Comp.Group, out var group))
            return;

        var format = group.FullName ? "name-identifier-format-full" : "name-identifier-format-append";
        // We apply the modifier with a low priority to keep it near the base name
        // "Beep (Si-4562) the zombie" instead of "Beep the zombie (Si-4562)"
        args.AddModifier(format, -10, ("identifier", ent.Comp.FullIdentifier));
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
            var name_proto = (NameIdentifierGroupPrototype)proto;

            // Only bother adding new ones.
            if (CurrentIds.ContainsKey(proto.ID))
                continue;

            var ids = GetOrCreateIdList(name_proto);
            FillGroup(name_proto, ids);
        }
    }


    private void CleanupIds(RoundRestartCleanupEvent ev)
    {
        EnsureIds();
    }
}
