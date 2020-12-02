using System;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedHumanoidAppearanceComponent : Component, IGenderable
    {
        private HumanoidCharacterAppearance _appearance;
        private Sex _sex;

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

        public Gender Gender => Sex switch
        {
            Sex.Female => Gender.Female,
            Sex.Male => Gender.Male,
            Sex.Classified => Gender.Neuter,
            _ => Gender.Epicene,
        };

        public override ComponentState GetComponentState()
        {
            return new HumanoidAppearanceComponentState(Appearance, Sex);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not HumanoidAppearanceComponentState cast)
                return;

            Appearance = cast.Appearance;
            Sex = cast.Sex;
        }

        public void UpdateFromProfile(ICharacterProfile profile)
        {
            var humanoid = (HumanoidCharacterProfile) profile;
            Appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
            Sex = humanoid.Sex;
        }

        [Serializable]
        [NetSerializable]
        private sealed class HumanoidAppearanceComponentState : ComponentState
        {
            public HumanoidAppearanceComponentState(HumanoidCharacterAppearance appearance, Sex sex) : base(ContentNetIDs.HUMANOID_APPEARANCE)
            {
                Appearance = appearance;
                Sex = sex;
            }

            public HumanoidCharacterAppearance Appearance { get; }
            public Sex Sex { get; }
        }
    }
}
