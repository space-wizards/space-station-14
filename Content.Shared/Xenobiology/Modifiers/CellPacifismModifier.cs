using Content.Shared.CombatMode.Pacification;
using Content.Shared.Xenobiology.Components;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology.Modifiers;

[Serializable, NetSerializable, UsedImplicitly]
public sealed partial class CellPacifismModifier : CellModifier
{
    public override void OnAdd(Entity<CellContainerComponent> ent, Cell cell, IEntityManager entityManager)
    {
        entityManager.EnsureComponent<PacifiedComponent>(ent);
    }
}
