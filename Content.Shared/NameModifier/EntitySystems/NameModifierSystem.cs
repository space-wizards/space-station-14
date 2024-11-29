using System.Linq;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.Components;

namespace Content.Shared.NameModifier.EntitySystems;

/// <inheritdoc cref="NameModifierComponent"/>
public sealed class NameModifierSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NameModifierComponent, EntityRenamedEvent>(OnEntityRenamed);
    }

    private void OnEntityRenamed(Entity<NameModifierComponent> ent, ref EntityRenamedEvent args)
    {
        SetBaseName(ent, args.NewName);
        RefreshNameModifiers((ent.Owner, ent.Comp));
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
        var meta = MetaData(entity);
        var baseName = meta.EntityName;
        if (Resolve(entity, ref entity.Comp, logMissing: false))
            baseName = entity.Comp.BaseName;

        // Raise an event to get any modifiers
        // If the entity already has the component, use its BaseName, otherwise use the entity's name from metadata
        var modifierEvent = new RefreshNameModifiersEvent(baseName);
        RaiseLocalEvent(entity, ref modifierEvent);

        // Nothing added a modifier, so we can just use the base name
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
        // We have at least one modifier, so we need to apply it to the entity.

        // Get the final name with modifiers applied
        var modifiedName = modifierEvent.GetModifiedName();

        // Add the component if needed, and initialize it with the base name
        if (!EnsureComp<NameModifierComponent>(entity, out var comp))
            SetBaseName((entity, comp), meta.EntityName);

        // Set the entity's name with modifiers applied
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

    private readonly List<(LocId LocId, int Priority, (string, object)[] ExtraArgs)> _modifiers = [];

    /// <inheritdoc/>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    /// <summary>
    /// How many modifiers have been added to this event.
    /// </summary>
    public int ModifierCount => _modifiers.Count;

    public RefreshNameModifiersEvent(string baseName)
    {
        BaseName = baseName;
    }

    /// <summary>
    /// Adds a modifier to the entity's name.
    /// The original name will be passed to Fluent as <c>$baseName</c> along with any <paramref name="extraArgs"/>.
    /// Modifiers with a higher <paramref name="priority"/> will be applied later.
    /// </summary>
    public void AddModifier(LocId locId, int priority = 0, params (string, object)[] extraArgs)
    {
        _modifiers.Add((locId, priority, extraArgs));
    }

    /// <summary>
    /// Returns the final name with all modifiers applied.
    /// </summary>
    public string GetModifiedName()
    {
        // Start out with the entity's name name
        var name = BaseName;

        // Iterate through all the modifiers in priority order
        foreach (var modifier in _modifiers.OrderBy(n => n.Priority))
        {
            // Grab any extra args needed by the Loc string
            var args = modifier.ExtraArgs;
            // Add the current version of the entity name as an arg
            Array.Resize(ref args, args.Length + 1);
            args[^1] = ("baseName", name);
            // Resolve the Loc string and use the result as the base in the next iteration.
            name = Loc.GetString(modifier.LocId, args);
        }

        return name;
    }
}
