using Content.Shared.Explosion.EntitySystems;

namespace Content.Client.Explosion;

public sealed partial class TriggerSystem : SharedTriggerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeProximity();
    }
}
