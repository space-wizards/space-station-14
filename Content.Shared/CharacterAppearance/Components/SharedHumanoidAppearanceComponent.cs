using System;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.CharacterAppearance.Components
{
    [NetworkedComponent()]
    public abstract class SharedHumanoidAppearanceComponent : Component
    {
        private HumanoidCharacterAppearance _appearance = HumanoidCharacterAppearance.Default();
        private Sex _sex;
        private Gender _gender;

        public sealed override string Name => "HumanoidAppearance";

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

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual HumanoidCharacterAppearance Appearance
        {
            get => _appearance;
            set
            {
                _appearance = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Sex Sex
        {
            get => _sex;
            set
            {
                _sex = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Gender Gender
        {
            get => _gender;
            set
            {
                _gender = value;

                if (Owner.TryGetComponent(out GrammarComponent? g))
                {
                    g.Gender = value;
                }

                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new HumanoidAppearanceComponentState(Appearance, Sex, Gender);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not HumanoidAppearanceComponentState cast)
                return;

            Appearance = cast.Appearance;
            Sex = cast.Sex;
            Gender = cast.Gender;
        }

        public void UpdateFromProfile(ICharacterProfile profile)
        {
            var humanoid = (HumanoidCharacterProfile) profile;
            Appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
            Sex = humanoid.Sex;
            Gender = humanoid.Gender;
        }

        [Serializable]
        [NetSerializable]
        private sealed class HumanoidAppearanceComponentState : ComponentState
        {
            public HumanoidAppearanceComponentState(HumanoidCharacterAppearance appearance, Sex sex, Gender gender)
            {
                Appearance = appearance;
                Sex = sex;
                Gender = gender;
            }

            public HumanoidCharacterAppearance Appearance { get; }
            public Sex Sex { get; }
            public Gender Gender { get; }
        }
    }
}
