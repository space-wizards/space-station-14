using System;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Melee
{
    [Prototype("MeleeWeaponAnimation")]
    public sealed class MeleeWeaponAnimationPrototype : IPrototype
    {
        [DataField("prototype")]
        private string _prototype = "WeaponArc";
        [DataField("state")]
        private string _state;
        [DataField("id")]
        private string _id;
        [DataField("colorDelta")]
        private Vector4 _colorDelta = Vector4.Zero;
        [DataField("color")]
        private Vector4 _color = new Vector4(1,1,1,1);
        [DataField("length")]
        private TimeSpan _length = TimeSpan.FromSeconds(0.5f);
        [DataField("speed")]
        private float _speed = 1;
        [DataField("width")]
        private float _width = 90;
        [DataField("arcType")]
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
