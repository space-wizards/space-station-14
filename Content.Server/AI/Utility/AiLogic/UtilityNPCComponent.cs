using Content.Server.AI.Components;
using Content.Server.AI.LoadBalancer;
using Content.Server.AI.Utility.Actions;
using Content.Server.AI.WorldState;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using System.Threading;
using Content.Server.AI.EntitySystems;

namespace Content.Server.AI.Utility.AiLogic
{
    [RegisterComponent, Access(typeof(NPCSystem))]
    [ComponentReference(typeof(NPCComponent))]
    public sealed class UtilityNPCComponent : NPCComponent
    {
        public Blackboard Blackboard => _blackboard;
        public Blackboard _blackboard = default!;

        /// <summary>
        ///     The sum of all BehaviorSets gives us what actions the AI can take
        /// </summary>
        [DataField("behaviorSets", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<BehaviorSetPrototype>))]
        public HashSet<string> BehaviorSets { get; } = new();

        public List<IAiUtility> AvailableActions { get; set; } = new();

        /// <summary>
        /// The currently running action; most importantly are the operators.
        /// </summary>
        public UtilityAction? CurrentAction { get; set; }

        /// <summary>
        /// How frequently we can re-plan. If an AI's in combat you could decrease the cooldown,
        /// or if there's no players nearby increase it.
        /// </summary>
        public float PlanCooldown { get; } = 0.5f;

        public float _planCooldownRemaining;

        /// <summary>
        /// If we've requested a plan then wait patiently for the action
        /// </summary>
        public AiActionRequestJob? _actionRequest;

        public CancellationTokenSource? _actionCancellation;
    }
}
