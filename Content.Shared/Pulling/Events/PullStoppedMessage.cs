namespace Content.Shared.Physics.Pull;
public sealed class PullStoppedMessage(EntityUid puller, EntityUid pulled) : PullMessage(puller, pulled) { }
