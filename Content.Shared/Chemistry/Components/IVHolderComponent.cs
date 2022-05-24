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

        /// <summary>
        /// What bags we're supposed to be attached to.
        /// </summary>
        [ViewVariables] public List<EntityUid>? Bags;


        /// <summary>
        /// Track a new bag.
        /// </summary>
        public void AddBag(EntityUid bag)
        {
            // Logger.Debug("[IV] IVHolderComponent - AddBag: " + bag);

            if (!(Bags ??= new()).Contains(bag))
                Bags.Add(bag);
        }

        /// <summary>
        /// Dereference a bag and delete self if empty.
        /// </summary>
        public void RemoveBag(EntityUid bag)
        {
            // Logger.Debug("[IV] IVHolderComponent - RemoveBag: " + bag);

            if (Bags == null || (Bags.Remove(bag) && Bags.Count == 0))
                _entMan.RemoveComponent<IVHolderComponent>(Owner);
        }

        /// <summary>
        /// Remove all invalid references to our bags.
        /// </summary>
        /// <param name="removeComp">If true: remove the component there are no bags.</param>
        public void ValidateBags(bool removeComp = false)
        {
            if (Deleted || Bags == null)
                return;

            for (int i = Bags.Count - 1; i >= 0; i--)
            {
                var bag = Bags[i];
                if (bag is not { Valid: true }
                    || !_entMan.HasComponent<SharedIVBagComponent>(bag))
                {
                    // Logger.Info("[IV] IVHolderComponent - removed invalid bag:" + bag);
                    Bags.RemoveAt(i);
                }
            }

            if (removeComp && Bags.Count == 0)
                _entMan.RemoveComponent<IVHolderComponent>(Owner);
        }
    }
}
