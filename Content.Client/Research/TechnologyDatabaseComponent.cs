using System;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Research
{
    [RegisterComponent]
    public sealed class TechnologyDatabaseComponent : SharedTechnologyDatabaseComponent
    {
        /// <summary>
        ///     Event called when the database is updated.
        /// </summary>
        public event Action? OnDatabaseUpdated;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not TechnologyDatabaseState state) return;

            _technologies.Clear();

            var protoManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var techID in state.Technologies)
            {
                if (!protoManager.TryIndex(techID, out TechnologyPrototype? technology)) continue;
                _technologies.Add(technology);
            }

            OnDatabaseUpdated?.Invoke();
        }
    }
}
