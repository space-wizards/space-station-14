using Content.Shared.Interaction;
using Content.Shared.Tools;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Construction.Components
{
    // TODO: Move this component's logic to an EntitySystem.
    [RegisterComponent, Friend(typeof(AnchorableSystem))]
    public class AnchorableComponent : Component
    {
        public override string Name => "Anchorable";

        [DataField("tool", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string Tool { get; private set; } = "Anchoring";

        [DataField("snap")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Snap { get; private set; } = true;
    }

    public abstract class BaseAnchoredAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid Tool { get; }

        /// <summary>
        ///     Extra delay to add to the do_after.
        ///     Add to this, don't replace it.
        ///     Output parameter.
        /// </summary>
        public float Delay { get; set; } = 0f;

        protected BaseAnchoredAttemptEvent(EntityUid user, EntityUid tool)
        {
            User = user;
            Tool = tool;
        }
    }

    public class AnchorAttemptEvent : BaseAnchoredAttemptEvent
    {
        public AnchorAttemptEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    public class UnanchorAttemptEvent : BaseAnchoredAttemptEvent
    {
        public UnanchorAttemptEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    public abstract class BaseAnchoredEvent : EntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid Tool { get; }

        protected BaseAnchoredEvent(EntityUid user, EntityUid tool)
        {
            User = user;
            Tool = tool;
        }
    }

    /// <summary>
    ///     Raised just before the entity's body type is changed.
    /// </summary>
    public class BeforeAnchoredEvent : BaseAnchoredEvent
    {
        public BeforeAnchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    public class AnchoredEvent : BaseAnchoredEvent
    {
        public AnchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    /// <summary>
    ///     Raised just before the entity's body type is changed.
    /// </summary>
    public class BeforeUnanchoredEvent : BaseAnchoredEvent
    {
        public BeforeUnanchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    public class UnanchoredEvent : BaseAnchoredEvent
    {
        public UnanchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }
}
