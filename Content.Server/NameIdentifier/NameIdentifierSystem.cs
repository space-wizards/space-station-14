using Content.Shared.GameTicking;
using Content.Shared.NameIdentifier;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.NameIdentifier;

/// <inheritdoc cref="NameIdentifierComponent"/>
public sealed partial class NameIdentifierSystem : SharedNameIdentifierSystem
{
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;

    /// <summary>
    /// Free IDs available per <see cref="NameIdentifierGroupPrototype"/>.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<string, List<int>> CurrentIds = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NameIdentifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NameIdentifierComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(CleanupIds);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnReloadPrototypes);

        InitialSetupPrototypes();
    }

    private void OnComponentShutdown(Entity<NameIdentifierComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Group is null)
            return;

        if (!CurrentIds.TryGetValue(ent.Comp.Group, out var ids))
        {
            _nameModifier.RefreshNameModifiers(ent.Owner);
            return;
        }

        // Not a valid value
        if (ent.Comp.Identifier == -1)
            return;

        if (ids.Count > 0)
        {
            // Avoid inserting the value right back at the end or shuffling in place:
            // just pick a random spot to put it and then move that one to the end.
            var randomIndex = _robustRandom.Next(ids.Count);
            var random = ids[randomIndex];
            ids[randomIndex] = ent.Comp.Identifier;
            ids.Add(random);
        }
        else
        {
            ids.Add(ent.Comp.Identifier);
        }

        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    /// <summary>
    /// <inheritdoc cref="GenerateUniqueNameModifier(Content.Shared.NameIdentifier.NameIdentifierGroupPrototype,out int)" path="/summary"/>
    /// </summary>
    /// <remarks>
    /// This overload resolves the ProtoId of the NameIdentifierGroupPrototype first.
    /// </remarks>
    /// <param name="proto">A ProtoId that will be resolved and passed on.</param>
    /// <param name="randomVal">The index value of the randomly selected modifier.</param>
    public string GenerateUniqueNameModifier(ProtoId<NameIdentifierGroupPrototype> proto, out int randomVal)
    {
        return GenerateUniqueNameModifier(ProtoMan.Index(proto), out randomVal);
    }

    /// <summary>
    /// Generates a new unique name modifier for a given entity and adds its index to <see cref="CurrentIds"/>
    /// but does not set the entity's name.
    /// </summary>
    /// <param name="proto">The <see cref="NameIdentifierGroupPrototype"/> prototype to retrieve from.</param>
    /// <param name="randomVal">The index value of the randomly selected modifier.</param>
    /// <returns>A formatted and/or localized modifier. Empty string if invalid.</returns>
    public string GenerateUniqueNameModifier(NameIdentifierGroupPrototype proto, out int randomVal)
    {
        randomVal = -1;
        if (!CurrentIds.TryGetValue(proto.ID, out var set))
            return string.Empty;

        if (set.Count == 0)
        {
            // Oh jeez. We're outta numbers.
            return string.Empty;
        }

        randomVal = set[^1];
        set.RemoveAt(set.Count - 1);

        return FormatAndLocalize(randomVal, proto);
    }

    /// <summary>
    /// Format and localize the provided integer against the prototype.
    /// </summary>
    /// <param name="value">The selected value to process.</param>
    /// <param name="proto">The prototype that defines optional localization and formatting.</param>
    /// <returns>A formatted and/or localized string.</returns>
    private string FormatAndLocalize(int value, NameIdentifierGroupPrototype proto)
    {
        var formatted = value.ToString();

        if (proto.IdentifierDataset is not null)
        {
            var identifiers = ProtoMan.Index(proto.IdentifierDataset);
            formatted = Loc.GetString(identifiers.Values.Prefix+formatted);
        }

        return proto.Format is not null
            ? Loc.GetString(proto.Format, ("number", formatted))
            : formatted;
    }

    /// <summary>
    /// Initializes the component when initialized on the map.
    /// This will use the existing identifier, if present, or generate a new one and update the component appropriately.
    /// </summary>
    /// <param name="ent">The entity component tuple being initialized.</param>
    /// <param name="args">The arguments for the event. Unused.</param>
    private void OnMapInit(Entity<NameIdentifierComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Group is null)
            return;

        if (!ProtoMan.Resolve(ent.Comp.Group, out var group))
            return;

        // If it has an existing valid identifier then use that, otherwise generate a new one.
        if (ent.Comp.Identifier != -1 &&
            CurrentIds.TryGetValue(ent.Comp.Group, out var ids) &&
            ids.Remove(ent.Comp.Identifier))
        {
            ent.Comp.FullIdentifier = FormatAndLocalize(ent.Comp.Identifier, group);
        }
        else
        {
            ent.Comp.FullIdentifier = GenerateUniqueNameModifier(group, out ent.Comp.Identifier);
        }

        Dirty(ent);
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void InitialSetupPrototypes()
    {
        EnsureIds();
    }

    /// <summary>
    /// Fill a provided list with a range of numbers corresponding to a prototype's defined range.
    /// </summary>
    /// <param name="proto">The <see cref="NameIdentifierGroupPrototype"/> prototype to retrieve from.</param>
    /// <param name="values">Reference to the list where values should be placed.</param>
    private void FillGroup(NameIdentifierGroupPrototype proto, List<int> values)
    {
        values.Clear();

        var (max, min) = (proto.MaxValue, proto.MinValue);

        if (proto.IdentifierDataset is not null)
        {
            max = ProtoMan.Index(proto.IdentifierDataset).Values.Count;
            min = 1;
        }

        for (var i = min; i <= max; i++)
        {
            values.Add(i);
        }

        _robustRandom.Shuffle(values);
    }

    /// <summary>
    /// Retrieve the appropriate list or create a new one if one does not already exist.
    /// </summary>
    /// <param name="proto">The <see cref="NameIdentifierGroupPrototype"/> prototype to retrieve from.</param>
    /// <returns>The list corresponding to the prototype, a new empty list if not already extant.</returns>
    private List<int> GetOrCreateIdList(NameIdentifierGroupPrototype proto)
    {
        // The ID list already exists.
        if (CurrentIds.TryGetValue(proto.ID, out var ids))
            return ids;

        // If we're using a dataset, grab the count. Otherwise, use (max - min).
        ids =  new List<int>(proto.IdentifierDataset is null
            ? proto.MaxValue - proto.MinValue
            : ProtoMan.Index(proto.IdentifierDataset).Values.Count);

        CurrentIds.Add(proto.ID, ids);
        return ids;
    }

    private void EnsureIds()
    {
        foreach (var proto in ProtoMan.EnumeratePrototypes<NameIdentifierGroupPrototype>())
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
            if (!ProtoMan.HasIndex<NameIdentifierGroupPrototype>(proto))
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
            var nameProto = (NameIdentifierGroupPrototype)proto;

            // Only bother adding new ones.
            if (CurrentIds.ContainsKey(proto.ID))
                continue;

            var ids = GetOrCreateIdList(nameProto);
            FillGroup(nameProto, ids);
        }
    }

    private void CleanupIds(RoundRestartCleanupEvent ev)
    {
        EnsureIds();
    }
}
