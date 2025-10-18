using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Binary.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.Piping.Binary.Systems;

public sealed class GasVolumePumpSystem : SharedGasVolumePumpSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasVolumePumpComponent, AfterAutoHandleStateEvent>(OnPumpState);
    }

    protected override void UpdateUi(Entity<GasVolumePumpComponent> entity)
    {
        if (_ui.TryGetOpenUi(entity.Owner, GasVolumePumpUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }

    private void OnPumpState(Entity<GasVolumePumpComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
