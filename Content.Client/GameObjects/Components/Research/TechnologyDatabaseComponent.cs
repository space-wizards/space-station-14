using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp.Processing;

namespace Content.Client.GameObjects.Components.Research
{
    [RegisterComponent]
    public class TechnologyDatabaseComponent : SharedTechnologyDatabaseComponent
    {
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is TechnologyDatabaseState state)) return;
            _technologies.Clear();
            foreach (var techID in state.Technologies)
            {
                if (!IoCManager.Resolve<IPrototypeManager>().TryIndex(techID, out TechnologyPrototype technology)) continue;
                _technologies.Add(technology);
            }

        }
    }
}
