using Content.Server.Arcade.Components;
using Content.Server.Power.Components;

namespace Content.Server.Arcade;

public sealed partial class ArcadeSystem
{
    private void InitializeSpaceVillain()
    {
        SubscribeLocalEvent<SpaceVillainArcadeComponent, PowerChangedEvent>(OnSVillainPower);
    }

    private void OnSVillainPower(EntityUid uid, SpaceVillainArcadeComponent component, ref PowerChangedEvent args)
    {
        component.OnPowerStateChanged(args);
    }
}
