using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;


namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Added to containers that have bags inserted into them.
    /// Acts as a MoveEvent proxy for a specific held bag.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class IVHolderComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public EntityUid? Bag;
        public TransformComponent? BagPos;


        public void SetBag(EntityUid bag)
        {
            Bag = bag;
            BagPos = _entMan.GetComponent<TransformComponent>(bag);
        }
    }
}
