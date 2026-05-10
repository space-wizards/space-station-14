using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Atmos.Piping.Trinary.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.Piping.Trinary.EntitySystems;

public sealed partial class GasMixerSystem : SharedGasMixerSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasMixerComponent, AfterAutoHandleStateEvent>(OnMixerState);
    }

    protected override void UpdateUi(Entity<GasMixerComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, GasMixerUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnMixerState(Entity<GasMixerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
