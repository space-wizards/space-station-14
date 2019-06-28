using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public enum AttackTargetDef
    {
        Head,
        Eyes,
        Mouth,
        Chest,
        LeftArm,
        RightArm,
        LeftHand,
        RightHand,
        Groin,
        LeftLeg,
        RightLeg,
        LeftFoot,
        RightFoot,
        SeveralTargets,
        All
    }
    public class Human : BodyTemplate
    {
        public Human(IEntity owner) : base(owner) { }

        public override void Initialize()
        {
            base.Initialize(owner); 
        }
    }
}
