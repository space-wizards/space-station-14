using Content.Server.GameObjects.Components.GUI;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States.Inventory
{
    /// <summary>
    /// AKA what's in active hand
    /// </summary>
    [UsedImplicitly]
    public sealed class EquippedEntityState : StateData<IEntity>
    {
        public override string Name => "EquippedEntity";

        public override IEntity GetValue()
        {
            if (!Owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return null;
            }

            return handsComponent.GetActiveHand?.Owner;
        }
    }
}
