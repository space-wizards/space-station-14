namespace Content.Shared.Movement.Pulling.Events;

/// <summary>
/// Event raised directed BOTH at the puller and pulled entity when a pull starts.
/// </summary>
public sealed class PullStoppedMessage(EntityUid pullerUid, EntityUid pulledUid) : PullMessage(pullerUid, pulledUid);
