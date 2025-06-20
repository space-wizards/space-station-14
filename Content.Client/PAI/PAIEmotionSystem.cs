using Content.Shared.PAI;
using Robust.Client.GameObjects;

namespace Content.Client.PAI;

public sealed class PAIEmotionSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIEmotionComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(EntityUid uid, PAIEmotionComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<PAIEmotionBoundUserInterface>(uid, PAIEmotionUiKey.Key, out var bui))
            bui.UpdateEmotion(component.CurrentEmotion);
    }
}
