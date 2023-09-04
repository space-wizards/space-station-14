namespace Content.Client.Explosion;

[InjectDependencies]
public sealed partial class TriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeProximity();
    }
}
