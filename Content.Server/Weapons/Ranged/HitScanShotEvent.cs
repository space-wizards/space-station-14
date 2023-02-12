namespace Content.Server.Weapons.Ranged;

public sealed class HitScanShotEvent : HandledEntityEventArgs
{

    public readonly EntityUid? User;

    /// <summary>
    ///     Shot may be redirected
    /// </summary>
    public EntityUid Target {get; set;}

    public HitScanShotEvent(EntityUid? user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}
