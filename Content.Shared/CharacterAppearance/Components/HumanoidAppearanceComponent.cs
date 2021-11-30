using System;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.CharacterAppearance.Components
{
    [RegisterComponent]
    [Friend(typeof(SharedHumanoidAppearanceSystem), typeof(SharedMagicMirrorComponent))]
    [NetworkedComponent]
    public class HumanoidAppearanceComponent : Component
    {
        public override string Name => "HumanoidAppearance";

        [ViewVariables]
        public HumanoidCharacterAppearance Appearance { get; set; } = HumanoidCharacterAppearance.Default();

        [ViewVariables(VVAccess.ReadWrite)]
        public Sex Sex { get; set; } = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public Gender Gender { get; set; } = default!;

        [DataField("categoriesHair")]
        [ViewVariables]
        public SpriteAccessoryCategories CategoriesHair { get; set; } = SpriteAccessoryCategories.HumanHair;

        [DataField("categoriesFacialHair")]
        [ViewVariables]
        public SpriteAccessoryCategories CategoriesFacialHair { get; set; } = SpriteAccessoryCategories.HumanFacialHair;

        [ViewVariables]
        [DataField("canColorHair")]
        public bool CanColorHair { get; set; } = true;

        [ViewVariables]
        [DataField("canColorFacialHair")]
        public bool CanColorFacialHair { get; set; } = true;
    }

    [Serializable, NetSerializable]
    public sealed class HumanoidAppearanceComponentState : ComponentState
    {
        public HumanoidCharacterAppearance Appearance { get; }
        public Sex Sex { get; }
        public Gender Gender { get; }

        public HumanoidAppearanceComponentState(HumanoidCharacterAppearance appearance,
            Sex sex,
            Gender gender)
        {
            Appearance = appearance;
            Sex = sex;
            Gender = gender;
        }
    }
}
