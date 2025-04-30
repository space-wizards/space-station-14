using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Clothing;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    private void InitializeBreathTool()
    {
        SubscribeLocalEvent<BreathToolComponent, ComponentShutdown>(OnBreathToolShutdown);
        SubscribeLocalEvent<BreathToolComponent, ItemMaskToggledEvent>(OnMaskToggled);
    }

    private void OnBreathToolShutdown(Entity<BreathToolComponent> entity, ref ComponentShutdown args)
    {
        DisconnectInternals(entity);
    }

    public void DisconnectInternals(Entity<BreathToolComponent> entity, bool forced = false)
    {
        var old = entity.Comp.ConnectedInternalsEntity;

        if (old == null)
            return;

        entity.Comp.ConnectedInternalsEntity = null;

        if (_internalsQuery.TryComp(old, out var internalsComponent))
        {
            _internals.DisconnectBreathTool((old.Value, internalsComponent), entity.Owner, forced: forced);
        }

        Dirty(entity);
    }

    private void OnMaskToggled(Entity<BreathToolComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (args.Mask.Comp.IsToggled)
        {
            DisconnectInternals(ent, forced: true);
        }
        else
        {
            if (_internalsQuery.TryComp(args.Wearer, out var internals))
            {
                _internals.ConnectBreathTool((args.Wearer.Value, internals), ent);
            }
        }
    }
}
