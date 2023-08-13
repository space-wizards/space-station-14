namespace Content.Server.Power.Events;

/// <summary>
/// Invoked on a target power provider when its power exceeds BreakerComponent MaxPower, popping the breaker or blowing the fuse.
/// </summary>
public sealed class BreakerPoppedEvent : EntityEventArgs
{
}
