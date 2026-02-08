using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Kitchen.EntitySystems;

namespace Content.Client.Kitchen.EntitySystems;

public sealed class ReagentGrinderSystem : SharedReagentGrinderSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReagentGrinderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ReagentGrinderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    public override void UpdateUi(EntityUid uid)
    {
        if (_userInterface.TryGetOpenUi(uid, ReagentGrinderUiKey.Key, out var bui))
            bui.Update();
    }
}
