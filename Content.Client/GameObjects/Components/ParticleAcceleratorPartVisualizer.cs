using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Singularity;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components
{
    public class ParticleAcceleratorPartVisualizer : AppearanceVisualizer
    {
        private Dictionary<ParticleAcceleratorVisualState, string> _states = new Dictionary<ParticleAcceleratorVisualState, string>();

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (!node.TryGetNode("baseState", out var baseStateNode))
            {
                throw new PrototypeLoadException("No baseState property specified for ParticleAcceleratorPartVisualizer");
            }

            var baseState = baseStateNode.AsString();
            _states.Add(ParticleAcceleratorVisualState.Open, baseState);
            _states.Add(ParticleAcceleratorVisualState.Wired, baseState+"w");
            _states.Add(ParticleAcceleratorVisualState.Closed, baseState+"c");
            _states.Add(ParticleAcceleratorVisualState.Powered, baseState+"p");
            _states.Add(ParticleAcceleratorVisualState.Level0, baseState+"0");
            _states.Add(ParticleAcceleratorVisualState.Level1, baseState+"1");
            _states.Add(ParticleAcceleratorVisualState.Level2, baseState+"2");
            _states.Add(ParticleAcceleratorVisualState.Level3, baseState+"3");
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            if (!entity.TryGetComponent<SpriteComponent>(out var sprite))
            {
                throw new EntityCreationException("No sprite component found in entity that has ParticleAcceleratorPartVisualizer");
            }

            if (!sprite.AllLayers.Any())
            {
                sprite.AddLayer(_states[ParticleAcceleratorVisualState.Closed]);
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            if (component.Owner.Deleted)
                return;

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite)) return;
            if (!component.TryGetData(ParticleAcceleratorVisuals.VisualState, out ParticleAcceleratorVisualState state))
            {
                state = ParticleAcceleratorVisualState.Closed;
            }

            if (!_states.ContainsKey(state))
            {
                Logger.Error($"ParticleAcceleratorPartVisualizer.OnChangeData did not find provided state in buffered state list (invalid value: {state})");
                return;
            }

            sprite.LayerSetState(0, _states[state]);
        }
    }
}
