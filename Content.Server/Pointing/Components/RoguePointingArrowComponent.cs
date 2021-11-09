using Content.Server.Pointing.EntitySystems;
using Content.Shared.Pointing.Components;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Pointing.Components
{
    [RegisterComponent]
    [Friend(typeof(RoguePointingSystem))]
    public class RoguePointingArrowComponent : SharedRoguePointingArrowComponent
    {
        [ViewVariables]
        public IEntity? Chasing;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("turningDelay")]
        public float TurningDelay = 2;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingSpeed")]
        public float ChasingSpeed = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingTime")]
        public float ChasingTime = 1;

        [DataField("explosionSound")]
        public SoundSpecifier ExplosionSound = new SoundCollectionSpecifier("explosion");
    }
}
