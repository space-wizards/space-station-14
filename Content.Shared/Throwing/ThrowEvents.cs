namespace Content.Shared.Throwing
{
    /// <summary>
    ///     Base class for all throw events.
    /// </summary>
    public abstract class ThrowEvent : HandledEntityEventArgs
    {
        public readonly EntityUid Thrown;
        public readonly EntityUid Target;
        public ThrownItemComponent Component;

        public ThrowEvent(EntityUid thrown, EntityUid target, ThrownItemComponent component)
        {
            Thrown = thrown;
            Target = target;
            Component = component;
        }
    }

    /// <summary>
    ///     Raised directed on the target entity being hit by the thrown entity.
    /// </summary>
    public sealed class ThrowHitByEvent : ThrowEvent
    {
        public ThrowHitByEvent(EntityUid thrown, EntityUid target, ThrownItemComponent component) : base(thrown, target, component)
        {
        }
    }

    /// <summary>
    ///     Raised directed on the thrown entity that hits another.
    /// </summary>
    public sealed class ThrowDoHitEvent : ThrowEvent
    {
        public readonly bool LandAfterFirstHit = true;
        public ThrowDoHitEvent(EntityUid thrown, EntityUid target, ThrownItemComponent component, bool landAfterHit) : base(thrown, target, component)
        {
            LandAfterFirstHit = landAfterHit;
        }
    }
}
