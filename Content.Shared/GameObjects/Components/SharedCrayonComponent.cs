using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components
{
    public class SharedCrayonComponent : Component
    {
        public override string Name => "Crayon";
        public override uint? NetID => ContentNetIDs.CRAYONS;

        public string SelectedState { get; set; }
        protected string _color;
    }

    [Serializable, NetSerializable]
    public enum CrayonVisuals
    {
        State,
        Color
    }

    [Serializable, NetSerializable]
    public class CrayonComponentState : ComponentState
    {
        public readonly string Color;
        public readonly string State;
        public CrayonComponentState(string color, string state) : base(ContentNetIDs.CRAYONS)
        {
            Color = color;
            State = state;
        }
    }

    [Serializable, NetSerializable, Prototype("crayonDecal")]
    public class CrayonDecalPrototype : IPrototype
    {
        private List<string> _decals;
        public List<string> Decals => _decals;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _decals, "decals", new List<string>());
        }
    }
}
