using Content.Shared.Cargo;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Cargo;

public class CargoTelepadVisualizer : AppearanceVisualizer
{
    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);

        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.TryGetComponent<SpriteComponent>(component.Owner, out var sprite)) return;

        component.TryGetData(CargoTelepadVisuals.State, out CargoTelepadState? state);

        switch (state)
        {
            case CargoTelepadState.Teleporting:
                // TODO: Play animation for 0.5s
                sprite.LayerSetState(0, "beam");
                break;
            case CargoTelepadState.Unpowered:
                sprite.LayerSetState(0, "offline");
                break;
            default:
                sprite.LayerSetState(0, "idle");
                break;
        }
    }
}
