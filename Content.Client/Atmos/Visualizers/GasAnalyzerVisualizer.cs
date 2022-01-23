using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public class GasAnalyzerVisualizer : AppearanceVisualizer
    {
        [DataField("state_off")]
        private string? _stateOff;
        [DataField("state_working")]
        private string? _stateWorking;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
            {
                return;
            }

            if (component.TryGetData(GasAnalyzerVisuals.VisualState, out GasAnalyzerVisualState visualState))
            {
                switch (visualState)
                {
                    case GasAnalyzerVisualState.Off:
                        sprite.LayerSetState(0, _stateOff);
                        break;
                    case GasAnalyzerVisualState.Working:
                        sprite.LayerSetState(0, _stateWorking);
                        break;
                }
            }
        }
    }
}
