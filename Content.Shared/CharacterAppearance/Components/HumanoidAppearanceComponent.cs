using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.CharacterAppearance.Components
{
    [RegisterComponent]
    public class HumanoidAppearanceComponent : Component
    {
        public override string Name => "HumanoidAppearance";

        [ViewVariables(VVAccess.ReadWrite)]
        public HumanoidCharacterAppearance Appearance = HumanoidCharacterAppearance.Default();

        [ViewVariables(VVAccess.ReadWrite)]
        public Sex Sex;

        [ViewVariables(VVAccess.ReadWrite)]
        public Gender Gender;

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
}
