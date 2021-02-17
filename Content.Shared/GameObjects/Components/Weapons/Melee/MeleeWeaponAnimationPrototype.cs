using System;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Weapons.Melee
{
    [Prototype("MeleeWeaponAnimation")]
    public sealed class MeleeWeaponAnimationPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("prototype")]
        private string _prototype = "WeaponArc";
        [YamlField("state")]
        private string _state;
        [YamlField("id")]
        private string _id;
        [YamlField("colorDelta")]
        private Vector4 _colorDelta = Vector4.Zero;
        [YamlField("color")]
        private Vector4 _color = new Vector4(1,1,1,1);
        [YamlField("length")]
        private TimeSpan _length = TimeSpan.FromSeconds(0.5f);
        [YamlField("speed")]
        private float _speed = 1;
        [YamlField("width")]
        private float _width = 90;
        [YamlField("arcType")]
        private WeaponArcType _arcType = WeaponArcType.Slash;

        [ViewVariables] public string ID => _id;
        [ViewVariables] public string State => _state;
        [ViewVariables] public string Prototype => _prototype;
        [ViewVariables] public TimeSpan Length => _length;
        [ViewVariables] public float Speed => _speed;
        [ViewVariables] public Vector4 Color => _color;
        [ViewVariables] public Vector4 ColorDelta => _colorDelta;
        [ViewVariables] public WeaponArcType ArcType => _arcType;
        [ViewVariables] public float Width => _width;
    }

    public enum WeaponArcType
    {
        Slash,
        Poke,
    }
}
