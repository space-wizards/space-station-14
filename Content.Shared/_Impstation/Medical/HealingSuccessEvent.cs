namespace Content.Shared._Impstation.Medical;

/// <summary>
/// This handles...
/// </summary>
public sealed class HealingSuccessEvent(EntityUid user, EntityUid target, EntityUid used) : EntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly EntityUid Target = target;
    public readonly EntityUid Used = used;
}
