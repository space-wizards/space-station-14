using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;


namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// Stores what IV bags this mob is connected to for distance checks.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class IVTargetComponent : Component
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
            Bags ??= new();

            if (!Bags.Contains(bag))
                Bags.Add(bag);
        }

        /// <summary>
        /// Dereference a bag and delete self if empty.
        /// </summary>
        public void RemoveBag(EntityUid bag)
        {
            if (Bags != null)
            {
                Bags.Remove(bag);

                if (Bags.Count == 0)
                    _entMan.RemoveComponent<IVTargetComponent>(Owner);
            }
            else
            {
                _entMan.RemoveComponent<IVTargetComponent>(Owner);
            }
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
                    Console.WriteLine("[IV] IVTargetComponent - removed invalid bag:" + bag);
                    Bags.RemoveAt(i);
                }
            }

            if (removeComp && Bags.Count == 0)
                _entMan.RemoveComponent<IVTargetComponent>(Owner);
        }
    }
}
