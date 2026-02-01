using Content.Server.Atmos.Piping.Trinary.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Trinary.Components;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class GasMixerSystem : SharedGasMixerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMixerComponent, AfterAutoHandleStateEvent>(OnValveUpdate);
    }

    private void OnValveUpdate(Entity<GasMixerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<GasMixerComponent> ent)
    {
        if (UserInterfaceSystem.TryGetOpenUi(ent.Owner, GasMixerUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
