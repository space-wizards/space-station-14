using System.Collections.Generic;
using System.Linq;
using Content.Shared.Singularity.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.ParticleAccelerator
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ParticleAcceleratorPartVisualizer : AppearanceVisualizer, ISerializationHooks
    {
        [DataField("baseState", required: true)]
        private string? _baseState;

        private Dictionary<ParticleAcceleratorVisualState, string> _states = new();

        void ISerializationHooks.AfterDeserialization()
        {
            if (_baseState == null)
            {
                return;
            }

            _states.Add(ParticleAcceleratorVisualState.Powered, _baseState + "p");
            _states.Add(ParticleAcceleratorVisualState.Level0, _baseState + "p0");
            _states.Add(ParticleAcceleratorVisualState.Level1, _baseState + "p1");
            _states.Add(ParticleAcceleratorVisualState.Level2, _baseState + "p2");
            _states.Add(ParticleAcceleratorVisualState.Level3, _baseState + "p3");
        }

        [Obsolete("Subscribe to your component being initialised instead.")]
        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<ISpriteComponent?>(entity, out var sprite))
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
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite)) return;
            if (!component.TryGetData(ParticleAcceleratorVisuals.VisualState, out ParticleAcceleratorVisualState state))
            {
                state = ParticleAcceleratorVisualState.Unpowered;
            }

            if (state != ParticleAcceleratorVisualState.Unpowered)
            {
                sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, true);
                sprite.LayerSetState(ParticleAcceleratorVisualLayers.Unlit, _states[state]);
            }
            else
            {
                sprite.LayerSetVisible(ParticleAcceleratorVisualLayers.Unlit, false);
            }
        }
    }
}
