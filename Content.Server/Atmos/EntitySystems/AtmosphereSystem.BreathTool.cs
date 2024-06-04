using Content.Server.Atmos.Components;
using Content.Server.Body.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private void InitializeBreathTool()
    {
        SubscribeLocalEvent<BreathToolComponent, ComponentShutdown>(OnBreathToolShutdown);
    }

    private void OnBreathToolShutdown(EntityUid uid, BreathToolComponent component, ComponentShutdown args)
    {
        DisconnectInternals(uid, component);
    }

    public void DisconnectInternals(EntityUid uid, BreathToolComponent component)
    {
        var old = component.ConnectedInternalsEntity;
        component.ConnectedInternalsEntity = null;

        if (TryComp<InternalsComponent>(old, out var internalsComponent))
        {
            _internals.DisconnectBreathTool((old.Value, internalsComponent), uid);
        }

        component.IsFunctional = false;
    }
}
