using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Changeling.Systems;

public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ChangelingIdentityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    public void UpdateUi(EntityUid uid)
    {
        if (_ui.TryGetOpenUi(uid, ChangelingTransformUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
