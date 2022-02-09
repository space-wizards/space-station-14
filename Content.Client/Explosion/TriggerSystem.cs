namespace Content.Client.Explosion;

public sealed partial class TriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeProximity();
    }
}
