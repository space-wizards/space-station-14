namespace Content.Shared.Physics.Pull;

public sealed class PullStartedMessage(EntityUid puller, EntityUid pulled) : PullMessage(puller, pulled) { }
