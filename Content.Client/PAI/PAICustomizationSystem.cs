using Content.Shared.PAI;
using Robust.Client.GameObjects;

namespace Content.Client.PAI;

public sealed class PAICustomizationSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAICustomizationComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(EntityUid uid, PAICustomizationComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<PAICustomizationBoundUserInterface>(uid, PAICustomizationUiKey.Key, out var bui))
            bui.UpdateEmotion(component.CurrentEmotion);
    }
}
