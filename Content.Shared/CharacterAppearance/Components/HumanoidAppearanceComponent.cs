using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.CharacterAppearance.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public class HumanoidAppearanceComponent : Component
    {
        public override string Name => "HumanoidAppearance";

        [ViewVariables]
        public HumanoidCharacterAppearance Appearance { get; set; } = HumanoidCharacterAppearance.Default();

        [ViewVariables]
        public Sex Sex { get; set; } = default!;

        [ViewVariables]
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
}
