#nullable enable
using System;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Weapons.Melee
{
    [Prototype("MeleeWeaponAnimation")]
    public sealed class MeleeWeaponAnimationPrototype : IPrototype
    {
        private string _prototype = "WeaponArc";
        private string _state = string.Empty;
        private string _id = string.Empty;
        private Vector4 _colorDelta;
        private Vector4 _color;
        private TimeSpan _length;
        private float _speed;
        private float _width;
        private WeaponArcType _arcType;

        [ViewVariables] public string ID => _id;
        [ViewVariables] public string State => _state;
        [ViewVariables] public string Prototype => _prototype;
        [ViewVariables] public TimeSpan Length => _length;
        [ViewVariables] public float Speed => _speed;
        [ViewVariables] public Vector4 Color => _color;
        [ViewVariables] public Vector4 ColorDelta => _colorDelta;
        [ViewVariables] public WeaponArcType ArcType => _arcType;
        [ViewVariables] public float Width => _width;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _prototype, "prototype", "WeaponArc");
            serializer.DataField(ref _state, "state", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _colorDelta, "colorDelta", Vector4.Zero);
            serializer.DataField(ref _color, "color", new Vector4(1, 1, 1, 1));
            if (serializer.TryReadDataField("length", out float length))
            {
                _length = TimeSpan.FromSeconds(length);
            }
            else
            {
                _length = TimeSpan.FromSeconds(0.5f);
            }

            serializer.DataField(ref _speed, "speed", 1);
            serializer.DataField(ref _arcType, "arcType", WeaponArcType.Slash);
            serializer.DataField(ref _width, "width", 90);
        }
    }

    public enum WeaponArcType
    {
        Slash,
        Poke,
    }
}
