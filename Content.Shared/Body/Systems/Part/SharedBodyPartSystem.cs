using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Systems.Part;

public abstract partial class SharedBodyPartSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeNetworking();
    }
}
