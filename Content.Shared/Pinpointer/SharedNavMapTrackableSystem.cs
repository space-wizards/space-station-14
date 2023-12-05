using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Pinpointer;

public sealed partial class SharedNavMapTrackableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NavMapTrackableComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<NavMapTrackableComponent, ComponentHandleState>(HandleCompState);
    }

    public void GetCompState(EntityUid uid, NavMapTrackableComponent component, ref ComponentGetState args)
    {
        var convertedParentUid = EntityManager.GetNetEntity(component.ParentUid);

        args.State = new NavMapTrackableComponentState
        {
            ParentUid = convertedParentUid,
            ChildOffsets = component.ChildOffsets,
        };
    }

    public void HandleCompState(EntityUid uid, NavMapTrackableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not NavMapTrackableComponentState state) return;

        var convertedParentUid = EntityManager.GetEntity(state.ParentUid);
        component.ParentUid = convertedParentUid;

        component.ChildOffsets = state.ChildOffsets;
    }
}
