using Content.Server.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Inventory
{
    /// <summary>
    /// AKA what's in active hand
    /// </summary>
    [UsedImplicitly]
    public sealed class EquippedEntityState : StateData<IEntity>
    {
        public override string Name => "EquippedEntity";

        public override IEntity? GetValue()
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out HandsComponent? handsComponent))
            {
                return null;
            }

            return handsComponent.GetActiveHand?.Owner;
        }
    }
}
