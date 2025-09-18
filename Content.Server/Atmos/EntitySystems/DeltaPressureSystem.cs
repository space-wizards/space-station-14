using Content.Server.Atmos.Components;
using Content.Shared.Examine;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// <para>System that handles <see cref="DeltaPressureComponent"/>.</para>
///
/// <para>Entities with a <see cref="DeltaPressureComponent"/> will take damage per atmostick
/// depending on the pressure they experience.</para>
///
/// <para>DeltaPressure logic is mostly handled in a partial class in Atmospherics.
/// This system handles the adding and removing of entities to a processing list,
/// as well as any field changes via the API.</para>
/// </summary>
public sealed class DeltaPressureSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeltaPressureComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DeltaPressureComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<DeltaPressureComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<DeltaPressureComponent, GridUidChangedEvent>(OnGridChanged);
    }

    private void OnComponentInit(Entity<DeltaPressureComponent> ent, ref ComponentInit args)
    {
        var xform = Transform(ent);
        if (xform.GridUid == null)
            return;

        EnsureComp<AirtightComponent>(ent);
        _atmosphereSystem.TryAddDeltaPressureEntity(xform.GridUid.Value, ent);
    }

    private void OnComponentShutdown(Entity<DeltaPressureComponent> ent, ref ComponentShutdown args)
    {
        // Wasn't part of a list, so nothing to clean up.
        if (ent.Comp.GridUid == null)
            return;

        _atmosphereSystem.TryRemoveDeltaPressureEntity(ent.Comp.GridUid.Value, ent);
    }

    private void OnExamined(Entity<DeltaPressureComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsTakingDamage)
            args.PushMarkup(Loc.GetString("window-taking-damage"));
    }

    private void OnGridChanged(Entity<DeltaPressureComponent> ent, ref GridUidChangedEvent args)
    {
        if (args.OldGrid != null)
        {
            _atmosphereSystem.TryRemoveDeltaPressureEntity(args.OldGrid.Value, ent);
        }

        if (args.NewGrid != null)
        {
            _atmosphereSystem.TryAddDeltaPressureEntity(args.NewGrid.Value, ent);
        }
    }
}
