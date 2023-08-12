using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using static Content.Shared.Humanoid.HumanoidAppearanceState;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.FixedPoint;

namespace Content.Shared.HeadSlime
{
    [RegisterComponent, NetworkedComponent]
    public sealed class HeadSlimeComponent : Component
    {    
        /// <summary>
        /// Are you a Head Slime Queen?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("HeadSlimeQueen")]
        public bool HeadSlimeQueen = false;    

        [ViewVariables(VVAccess.ReadWrite)]
        public float HeadSlimeMovementSpeedDebuff = 0.80f;
        
        [ViewVariables(VVAccess.ReadWrite), DataField("infectTime")]
        public float InfectTime =  9f; 

        [ViewVariables(VVAccess.ReadWrite), DataField("injectTime")]
        public float InjectTime =  1.5f; 

        /// <summary>
        /// The role prototype of the HeadSlime antag role
        /// </summary>
        [DataField("headSlimeRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
        public readonly string HeadSlimeRoleId = "HeadSlime";

        [DataField("headSlimeStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
        public string HeadSlimeStatusIcon = "HeadSlimeFaction";

        /// <summary>
        ///     Path to antagonist alert sound.
        /// </summary>
        [DataField("greetSoundNotification")]
        public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/HeadSlime_start.ogg");
        
        /// <summary>
        ///     The action for the infect ability
        /// </summary>
        [DataField("HeadSlimeInfect")]
        public EntityTargetAction? HeadSlimeInfect = new();
        
        /// <summary>
        ///     The action for the inject ability
        /// </summary>
        [DataField("HeadSlimeInject")]
        public EntityTargetAction? HeadSlimeInject = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bSInfectActionName")]
        public string BSInfectActionName = "HeadSlimeInfectAction";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bSInjectActionName")]
        public string BSInjectActionName = "HeadSlimeInjectAction";
    }
    
public sealed class HeadSlimeInfectEvent : EntityTargetActionEvent { }
public sealed class HeadSlimeInjectEvent : EntityTargetActionEvent { }

}
