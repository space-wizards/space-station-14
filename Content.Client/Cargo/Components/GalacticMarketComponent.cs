using System;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Cargo.Components
{
    [RegisterComponent]
    public sealed class GalacticMarketComponent : SharedGalacticMarketComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     Event called when the database is updated.
        /// </summary>
        public event Action? OnDatabaseUpdated;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not GalacticMarketState state)
                return;
            _productIds.Clear();
            foreach (var productId in state.Products)
            {
                if (!_prototypeManager.TryIndex(productId, out CargoProductPrototype? product))
                    continue;
                _products.Add(product);
            }

            OnDatabaseUpdated?.Invoke();
        }
    }
}
