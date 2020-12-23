using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    [UsedImplicitly]
    public class ParticleAcceleratorPartVisualizer : AppearanceVisualizer
    {
        private readonly Dictionary<ParticleAcceleratorVisualState, string> _states = new();

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            var serializer = YamlObjectSerializer.NewReader(node);
            if (!serializer.TryReadDataField<string>("baseState", out var baseState))
            {
                throw new PrototypeLoadException("No baseState property specified for ParticleAcceleratorPartVisualizer");
            }

            _states.Add(ParticleAcceleratorVisualState.Powered, baseState+"p");
            _states.Add(ParticleAcceleratorVisualState.Level0, baseState+"p0");
            _states.Add(ParticleAcceleratorVisualState.Level1, baseState+"p1");
            _states.Add(ParticleAcceleratorVisualState.Level2, baseState+"p2");
            _states.Add(ParticleAcceleratorVisualState.Level3, baseState+"p3");
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            if (!entity.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                throw new EntityCreationException("No sprite component found in entity that has ParticleAcceleratorPartVisualizer");
            }

            if (!sprite.AllLayers.Any())
            {
                throw new EntityCreationException("No Layer set for entity that has ParticleAcceleratorPartVisualizer");
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
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
