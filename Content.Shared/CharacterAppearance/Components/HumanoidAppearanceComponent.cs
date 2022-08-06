using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Preferences;
using Content.Shared.Species;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance.Components
{
    [RegisterComponent]
    [Access(typeof(SharedHumanoidAppearanceSystem), typeof(SharedMagicMirrorComponent))]
    [NetworkedComponent]
    public sealed class HumanoidAppearanceComponent : Component
    {
        [ViewVariables]
        public HumanoidCharacterAppearance Appearance { get; set; } = HumanoidCharacterAppearance.Default();

        [ViewVariables(VVAccess.ReadWrite)]
        public Sex Sex { get; set; } = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public Gender Gender { get; set; } = default!;

        [ViewVariables]
        public string Species { get; set; } = SpeciesManager.DefaultSpecies;

        [ViewVariables(VVAccess.ReadWrite)]
        public int Age { get; set; } = HumanoidCharacterProfile.MinimumAge;

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

        [ViewVariables]
        [DataField("hairMatchesSkin")]
        public bool HairMatchesSkin { get; set; } = false;

        [ViewVariables]
        [DataField("hairAlpha")]
        public float HairAlpha { get; set; } = 1.0f;
    }

    [Serializable, NetSerializable]
    public sealed class HumanoidAppearanceComponentState : ComponentState
    {
        public HumanoidCharacterAppearance Appearance { get; }
        public Sex Sex { get; }
        public Gender Gender { get; }
        public string Species { get; }
        public int Age { get; }

        public HumanoidAppearanceComponentState(HumanoidCharacterAppearance appearance,
            Sex sex,
            Gender gender,
            string species,
            int age)
        {
            Appearance = appearance;
            Sex = sex;
            Gender = gender;
            Species = species;
            Age = age;
        }
    }
}
