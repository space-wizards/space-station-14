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
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("state")]
        public string State { get; } = string.Empty;

        [ViewVariables]
        [field: DataField("prototype")]
        public string Prototype { get; } = "WeaponArc";

        [ViewVariables]
        [field: DataField("length")]
        public TimeSpan Length { get; } = TimeSpan.FromSeconds(0.5f);

        [ViewVariables]
        [field: DataField("speed")]
        public float Speed { get; } = 1;

        [ViewVariables]
        [field: DataField("color")]
        public Vector4 Color { get; } = new(1,1,1,1);

        [ViewVariables]
        [field: DataField("colorDelta")]
        public Vector4 ColorDelta { get; } = Vector4.Zero;

        [ViewVariables]
        [field: DataField("arcType")]
        public WeaponArcType ArcType { get; } = WeaponArcType.Slash;

        [ViewVariables]
        [field: DataField("width")]
        public float Width { get; } = 90;
    }

    public enum WeaponArcType
    {
        Slash,
        Poke,
    }
}
