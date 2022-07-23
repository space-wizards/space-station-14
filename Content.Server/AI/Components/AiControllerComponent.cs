using Content.Server.AI.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.AI.Components
{
    [RegisterComponent]
    [Virtual]
    public class AiControllerComponent : Component
    {
        [DataField("logic")] private float _visionRadius = 8.0f;

        // TODO: Need to ECS a lot more of the AI first before we can ECS this
        /// <summary>
        /// Whether the AI is actively iterated.
        /// </summary>
        public bool Awake
        {
            get => _awake;
            set
            {
                if (_awake == value) return;

                _awake = value;

               if (_awake)
                   EntitySystem.Get<NPCSystem>().WakeNPC(this);
               else
                   EntitySystem.Get<NPCSystem>().SleepNPC(this);
            }
        }

        [DataField("awake")]
        private bool _awake = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public float VisionRadius
        {
            get => _visionRadius;
            set => _visionRadius = value;
        }

        /// <summary>
        ///     Is the entity Sprinting (running)?
        /// </summary>
        [ViewVariables]
        public bool Sprinting { get; } = true;

        /// <summary>
        ///     Calculated linear velocity direction of the entity.
        /// </summary>
        [ViewVariables]
        public Vector2 VelocityDir { get; set; }

        public virtual void Update(float frameTime) {}
    }
}
