using System;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedLooksComponent : Component
    {
        private HumanoidCharacterAppearance _appearance;
        private Sex _sex;

        public sealed override string Name => "Hair";
        public sealed override uint? NetID => ContentNetIDs.HAIR;
        public sealed override Type StateType => typeof(LooksComponentState);

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

        public override ComponentState GetComponentState()
        {
            return new LooksComponentState(Appearance, Sex);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cast = (LooksComponentState) curState;
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
        private sealed class LooksComponentState : ComponentState
        {
            public LooksComponentState(HumanoidCharacterAppearance appearance, Sex sex) : base(ContentNetIDs.HAIR)
            {
                Appearance = appearance;
                Sex = sex;
            }

            public HumanoidCharacterAppearance Appearance { get; }
            public Sex Sex { get; }
        }
    }
}
