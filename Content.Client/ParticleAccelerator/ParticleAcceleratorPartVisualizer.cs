using System.Linq;
using Content.Shared.Singularity.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.ParticleAccelerator
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ParticleAcceleratorPartVisualizer : AppearanceVisualizer
    {
        [DataField("baseState", required: true)]
        private string _baseState = default!;

        private static readonly Dictionary<ParticleAcceleratorVisualState, string> StatesSuffixes = new()
        {
            {ParticleAcceleratorVisualState.Powered, "p"},
            {ParticleAcceleratorVisualState.Level0, "p0"},
            {ParticleAcceleratorVisualState.Level1, "p1"},
            {ParticleAcceleratorVisualState.Level2, "p2"},
            {ParticleAcceleratorVisualState.Level3, "p3"},
        };

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<SpriteComponent?>(entity, out var sprite))
            {
                throw new EntityCreationException("No sprite component found in entity that has ParticleAcceleratorPartVisualizer");
            }

            if (!sprite.AllLayers.Any())
            {
                throw new EntityCreationException("No Layer set for entity that has ParticleAcceleratorPartVisualizer");
            }
        }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite)) return;
            if (!component.TryGetData(ParticleAcceleratorVisuals.VisualState, out ParticleAcceleratorVisualState state))
            {
                state = ParticleAcceleratorVisualState.Unpowered;
            }

            if (state != ParticleAcceleratorVisualState.Unpowered)
            {
                sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, true);
                sprite.LayerSetState(ParticleAcceleratorVisualLayers.Unlit, _baseState + StatesSuffixes[state]);
            }
            else
            {
                sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, false);
            }
        }
    }
}
