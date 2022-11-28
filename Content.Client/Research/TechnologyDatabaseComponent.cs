using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
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

            TechnologyIds.Clear();

            var protoManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var techID in state.Technologies)
            {
                if (!protoManager.HasIndex<TechnologyPrototype>(techID)) continue;
                TechnologyIds.Add(techID);
            }

            OnDatabaseUpdated?.Invoke();
        }
    }
}
