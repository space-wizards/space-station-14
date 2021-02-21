using System;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedHumanoidAppearanceComponent : Component, IGenderable
    {
        private HumanoidCharacterAppearance _appearance;
        private Sex _sex;
        private Gender _gender;

        public sealed override string Name => "HumanoidAppearance";
        public sealed override uint? NetID => ContentNetIDs.HUMANOID_APPEARANCE;

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
                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new HumanoidAppearanceComponentState(Appearance, Sex, Gender);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
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
            public HumanoidAppearanceComponentState(HumanoidCharacterAppearance appearance, Sex sex, Gender gender) : base(ContentNetIDs.HUMANOID_APPEARANCE)
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
