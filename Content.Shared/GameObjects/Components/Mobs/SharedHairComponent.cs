using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedHairComponent : Component
    {
        private string _facialHairStyleName;
        private string _hairStyleName;
        private Color _hairColor;
        private Color _facialHairColor;

        public sealed override string Name => "Hair";
        public sealed override uint? NetID => ContentNetIDs.HAIR;
        public sealed override Type StateType => typeof(HairComponentState);

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual string HairStyleName
        {
            get => _hairStyleName;
            set
            {
                _hairStyleName = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual string FacialHairStyleName
        {
            get => _facialHairStyleName;
            set
            {
                _facialHairStyleName = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Color HairColor
        {
            get => _hairColor;
            set
            {
                _hairColor = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Color FacialHairColor
        {
            get => _facialHairColor;
            set
            {
                _facialHairColor = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new HairComponentState(HairStyleName, FacialHairStyleName, HairColor, FacialHairColor);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cast = (HairComponentState) curState;

            HairStyleName = cast.HairStyleName;
            FacialHairStyleName = cast.FacialHairStyleName;
            HairColor = cast.HairColor;
            FacialHairColor = cast.FacialHairColor;
        }

        [Serializable, NetSerializable]
        private sealed class HairComponentState : ComponentState
        {
            public string HairStyleName { get; }
            public string FacialHairStyleName { get; }
            public Color HairColor { get; }
            public Color FacialHairColor { get; }

            public HairComponentState(string hairStyleName, string facialHairStyleName, Color hairColor, Color facialHairColor) : base(ContentNetIDs.HAIR)
            {
                HairStyleName = hairStyleName;
                FacialHairStyleName = facialHairStyleName;
                HairColor = hairColor;
                FacialHairColor = facialHairColor;
            }
        }
    }
}
