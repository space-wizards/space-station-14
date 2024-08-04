using Content.Shared.Atmos.Components;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class PipeColorSyncSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipeColorSyncComponent, AfterAutoHandleStateEvent>(OnPipeAfterHandleState);
    }

    private void OnPipeAfterHandleState(EntityUid uid, PipeColorSyncComponent component, ref AfterAutoHandleStateEvent args)
    {
        
    }
}
