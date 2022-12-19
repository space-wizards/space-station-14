using Content.Shared.Gravity;

namespace Content.Client.Gravity;

public sealed partial class GravitySystem : SharedGravitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        InitializeShake();
    }
}
