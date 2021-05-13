#nullable enable
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
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("state")]
        public string State { get; } = string.Empty;

        [ViewVariables]
        [DataField("prototype")]
        public string Prototype { get; } = "WeaponArc";

        [ViewVariables]
        [DataField("length")]
        public TimeSpan Length { get; } = TimeSpan.FromSeconds(0.5f);

        [ViewVariables]
        [DataField("speed")]
        public float Speed { get; } = 1;

        [ViewVariables]
        [DataField("color")]
        public Vector4 Color { get; } = new(1,1,1,1);

        [ViewVariables]
        [DataField("colorDelta")]
        public Vector4 ColorDelta { get; } = Vector4.Zero;

        [ViewVariables]
        [DataField("arcType")]
        public WeaponArcType ArcType { get; } = WeaponArcType.Slash;

        [ViewVariables]
        [DataField("width")]
        public float Width { get; } = 90;
    }

    public enum WeaponArcType
    {
        Slash,
        Poke,
    }
}
