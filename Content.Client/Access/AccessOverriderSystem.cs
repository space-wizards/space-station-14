using Content.Client.Access.UI;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

namespace Content.Client.Access;

public sealed class AccessOverriderSystem : SharedAccessOverriderSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessOverriderComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<AccessOverriderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        DirtyUI(ent);
    }

    protected override void DirtyUI(EntityUid uid)
    {
        if (UI.TryGetOpenUi<AccessOverriderBoundUserInterface>(uid, AccessOverriderUiKey.Key, out var bui))
            bui.Update();
    }
}
