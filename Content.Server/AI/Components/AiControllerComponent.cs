using Content.Server.AI.EntitySystems;

namespace Content.Server.AI.Components
{
    [RegisterComponent]
    [Virtual]
    public class AiControllerComponent : Component
    {
        [DataField("logic")] private float _visionRadius = 8.0f;

        public bool CanMove { get; set; } = true;

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
    }
}
