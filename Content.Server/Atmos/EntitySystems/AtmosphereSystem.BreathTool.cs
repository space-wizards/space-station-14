using Content.Server.Atmos.Components;
using Content.Server.Body.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private void InitializeBreathTool()
    {
        SubscribeLocalEvent<BreathToolComponent, ComponentShutdown>(OnBreathToolShutdown);
    }

    private void OnBreathToolShutdown(Entity<BreathToolComponent> entity, ref ComponentShutdown args)
    {
        DisconnectInternals(entity);
    }

    public void DisconnectInternals(Entity<BreathToolComponent> entity)
    {
        var old = entity.Comp.ConnectedInternalsEntity;
        entity.Comp.ConnectedInternalsEntity = null;

        if (TryComp<InternalsComponent>(old, out var internalsComponent))
        {
            _internals.DisconnectBreathTool((old.Value, internalsComponent), entity.Owner);
        }

        entity.Comp.IsFunctional = false;
    }
}
