using System.Runtime.CompilerServices;
using Content.Shared.Xenobiology.Components.Container;

namespace Content.Shared.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    /// <summary>
    /// Applies the <see cref="CellModifier.OnAdd"/> of all cell modifiers to an entity.
    /// </summary>
    public void ApplyAddCellModifiers(Entity<CellContainerComponent> entity, Cell cell)
    {
        foreach (var modifier in GetCellModifiersEnumerator(entity, cell))
        {
            modifier.OnAdd(entity, cell, EntityManager);
        }
    }

    /// <summary>
    /// Applies the <see cref="CellModifier.OnRemove"/> of all cell modifiers to an entity.
    /// </summary>
    public void ApplyRemoveCellModifiers(Entity<CellContainerComponent> entity, Cell cell)
    {
        foreach (var modifier in GetCellModifiersEnumerator(entity, cell))
        {
            modifier.OnRemove(entity, cell, EntityManager);
        }
    }

    /// <summary>
    /// Return <see cref="IEnumerable{T}"/> with all <see cref="CellModifier"/> from the cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<CellModifier> GetCellModifiersEnumerator(Entity<CellContainerComponent> entity, Cell cell)
    {
        foreach (var modifierId in cell.Modifiers)
        {
            if (!_prototype.TryIndex(modifierId, out var modifierProto))
            {
                Log.Error($"Enumerate modifiers prototype with nonexistent id {modifierId}");
                continue;
            }

            foreach (var modifier in modifierProto.Modifiers)
            {
                yield return modifier;
            }
        }
    }
}
