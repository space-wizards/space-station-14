using Content.Shared.Construction.EntitySystems;
using Content.Shared.Tools;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Components
{
    [RegisterComponent, Access(typeof(AnchorableSystem))]
    public sealed partial class AnchorableComponent : Component
    {
        [DataField("tool", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string Tool { get; private set; } = "Anchoring";

        [DataField("snap")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Snap { get; private set; } = true;

        /// <summary>
        /// Base delay to use for anchoring.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 1f;
    }

    public abstract class BaseAnchoredAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Tool;

        public readonly EntityUid GridUid;
        public readonly Vector2i GridIndex;

        /// <summary>
        ///     Extra delay to add to the do_after.
        ///     Add to this, don't replace it.
        ///     Output parameter.
        /// </summary>
        public float Delay = 0f;

        protected BaseAnchoredAttemptEvent(EntityUid user, EntityUid tool, EntityUid gridUid, Vector2i gridIndex)
        {
            User = user;
            Tool = tool;
            GridIndex = gridIndex;
        }
    }

    public sealed class AnchorAttemptEvent : BaseAnchoredAttemptEvent
    {
        /// <summary>
        /// Final rotation of the desired anchoring.
        /// </summary>
        public readonly Angle LocalRotation;

        public AnchorAttemptEvent(EntityUid user, EntityUid tool, Angle localRotation, EntityUid gridUid,
            Vector2i gridIndex) : base(user, tool, gridUid, gridIndex)
        {
            LocalRotation = localRotation;
        }
    }

    public sealed class UnanchorAttemptEvent : BaseAnchoredAttemptEvent
    {
        public UnanchorAttemptEvent(EntityUid user, EntityUid tool, EntityUid gridUid, Vector2i tileRef) : base(user, tool, gridUid, tileRef) { }
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
    public sealed class BeforeAnchoredEvent : BaseAnchoredEvent
    {
        public BeforeAnchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    /// <summary>
    ///     Raised when an entity with an anchorable component is anchored. Note that you may instead want the more
    ///     general <see cref="AnchorStateChangedEvent"/>. This event has the benefit of having user & tool information,
    ///     as a result of interactions mediated by the <see cref="AnchorableSystem"/>.
    /// </summary>
    public sealed class UserAnchoredEvent : BaseAnchoredEvent
    {
        public UserAnchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    /// <summary>
    ///     Raised just before the entity's body type is changed.
    /// </summary>
    public sealed class BeforeUnanchoredEvent : BaseAnchoredEvent
    {
        public BeforeUnanchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }

    /// <summary>
    ///     Raised when an entity with an anchorable component is unanchored. Note that you will probably also need to
    ///     subscribe to the more general <see cref="AnchorStateChangedEvent"/>, which gets raised BEFORE this one. This
    ///     event has the benefit of having user & tool information, whereas the more general event may be due to
    ///     explosions or grid-destruction or other interactions not mediated by the <see cref="AnchorableSystem"/>.
    /// </summary>
    public sealed class UserUnanchoredEvent : BaseAnchoredEvent
    {
        public UserUnanchoredEvent(EntityUid user, EntityUid tool) : base(user, tool) { }
    }
}
