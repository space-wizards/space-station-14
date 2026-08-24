using Content.Shared.Atmos.AirlockController;

namespace Content.Client.Atmos.AirlockController;

public sealed partial class AirlockControllerSystem : SharedAirlockControllerSystem
{
    [SubscribeLocalEvent]
    private void OnConfigChanged(Entity<AirlockControllerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<AirlockControllerComponent> ent)
    {
        if (UserInterfaceSystem.TryGetOpenUi(ent.Owner, AirlockControllerUiKey.Config, out var bui))
            bui.Update();
    }
}
