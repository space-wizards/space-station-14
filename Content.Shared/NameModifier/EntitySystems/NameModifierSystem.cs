using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.Components;
using Robust.Shared.Collections;

namespace Content.Shared.NameModifier.EntitySystems;

public sealed partial class NameModifierSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NameModifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NameModifierComponent, EntityRenamedEvent>(OnEntityRenamed);
    }

    private void OnMapInit(Entity<NameModifierComponent> entity, ref MapInitEvent args)
    {
        //SetBaseName(entity, Name(entity));
    }

    private void OnEntityRenamed(Entity<NameModifierComponent> entity, ref EntityRenamedEvent args)
    {
        SetBaseName((entity, entity.Comp), args.NewName);
        RefreshNameModifiers((entity, entity.Comp));
    }

    private void SetBaseName(Entity<NameModifierComponent> entity, string name)
    {
        if (name == entity.Comp.BaseName)
            return;

        // Set the base name to the new name
        entity.Comp.BaseName = name;
        Dirty(entity);
    }

    /// <summary>
    /// Raises a <see cref="RefreshNameModifiersEvent"/> to gather modifiers and
    /// updates the entity's name to its base name with modifiers applied.
    /// This will add a <see cref="NameModifierComponent"/> if any modifiers are added.
    /// </summary>
    /// <remarks>
    /// Call this to update the entity's name when adding or removing a modifier.
    /// </remarks>
    public void RefreshNameModifiers(Entity<NameModifierComponent?> entity)
    {
        Resolve(entity, ref entity.Comp, logMissing: false);

        var meta = MetaData(entity);

        // Raise an event to get any modifiers
        var modifierEvent = new RefreshNameModifiersEvent(entity.Comp?.BaseName ?? meta.EntityName);
        RaiseLocalEvent(entity, ref modifierEvent);

        // No modifiers
        if (modifierEvent.ModifierCount == 0)
        {
            // If the entity doesn't have the component, we're done
            if (entity.Comp == null)
                return;

            // Restore the base name
            _metaData.SetEntityName(entity, entity.Comp.BaseName, meta, raiseEvents: false);
            // The component isn't doing anything anymore, so remove it
            RemComp<NameModifierComponent>(entity);
            return;
        }

        // Get the final name with modifiers applied
        var modifiedName = modifierEvent.GetModifiedName();

        // Add the component if needed, and store the base name
        if (!EnsureComp<NameModifierComponent>(entity, out var comp))
            SetBaseName((entity, comp), meta.EntityName);

        // Set the entity's name with modifiers
        _metaData.SetEntityName(entity, modifiedName, meta, raiseEvents: false);
    }
}

/// <summary>
/// Raised on an entity when <see cref="NameModifierSystem.RefreshNameModifiers"/> is called.
/// Subscribe to this event and use its methods to add modifiers to the entity's name.
/// </summary>
[ByRefEvent]
public sealed class RefreshNameModifiersEvent : IInventoryRelayEvent
{
    /// <summary>
    /// The entity's name without any modifiers applied.
    /// If you want to base a modifier on the entity's name, use
    /// this so you don't include other modifiers.
    /// </summary>
    public readonly string BaseName;

    private readonly List<(LocId LocId, int Priority, ValueList<(string, object)>? ExtraArgs)> _modifiers = [];

    /// <inheritdoc/>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public int ModifierCount => _modifiers.Count;

    public RefreshNameModifiersEvent(string baseName)
    {
        BaseName = baseName;
    }

    /// <summary>
    /// Adds a modifier to the entity's name.
    /// The original name will be passed to Fluent as <c>$baseName</c> along with any <paramref name="extraArgs"/>.
    /// Prefixes with a higher <paramref name="priority"/> will be applied later.
    /// </summary>
    public void AddModifier(LocId locId, int priority = 0, ValueList<(string, object)>? extraArgs = null)
    {
        _modifiers.Add((locId, priority, extraArgs));
    }

    /// <summary>
    /// Returns the final name with all modifiers applied.
    /// </summary>
    public string GetModifiedName()
    {
        var name = BaseName;

        foreach (var modifier in _modifiers.OrderBy(n => n.Priority))
        {
            var args = modifier.ExtraArgs ?? [];
            args.Add(("baseName", name));
            name = Loc.GetString(modifier.LocId, args.ToArray());
        }

        return name;
    }
}
