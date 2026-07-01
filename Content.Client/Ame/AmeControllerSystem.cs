using Content.Shared.Ame;
using Content.Shared.Ame.Components;
using Content.Shared.Ame.Systems;

namespace Content.Client.Ame;

public sealed partial class AmeControllerSystem : SharedAmeControllerSystem
{
    public override void UpdateUi(Entity<AmeControllerComponent?> ent)
    {
        if (!UISystem.TryGetOpenUi(ent.Owner, AmeControllerUiKey.Key, out var bui))
            return;

        bui.Update();
    }
}
