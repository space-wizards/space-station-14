namespace Content.Shared.Smoking;

public abstract partial class SharedSmokingSystem : EntitySystem
{
    public override void Initialize()
    {
        InitializeVapes();
    }
}
