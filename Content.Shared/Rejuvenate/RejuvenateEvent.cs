namespace Content.Shared.Rejuvenate;

/// <summary>
/// Raised when an entity is supposed to be rejuvenated,
/// meaning it should heal all damage, debuffs or other negative status effects.
/// Systems should handle healing the entity in a subscription to this event.
/// Used for the Rejuvenate admin verb.
/// </summary>
public sealed class RejuvenateEvent : EntityEventArgs;
