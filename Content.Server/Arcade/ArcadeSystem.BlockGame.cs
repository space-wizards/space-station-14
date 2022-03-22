using Content.Server.Arcade.Components;
using Content.Server.Power.Components;

namespace Content.Server.Arcade;

public sealed partial class ArcadeSystem
{
    private void InitializeBlockGame()
    {
        SubscribeLocalEvent<BlockGameArcadeComponent, PowerChangedEvent>(OnBlockPowerChanged);
    }

    private static void OnBlockPowerChanged(EntityUid uid, BlockGameArcadeComponent component, ref PowerChangedEvent args)
    {
        component.OnPowerStateChanged(args);
    }
}
