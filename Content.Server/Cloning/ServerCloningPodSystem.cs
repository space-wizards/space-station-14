using Content.Server.Atmos.EntitySystems;
using Content.Server.EUI;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cloning;

/// <inheritdoc/>
public sealed partial class ServerCloningPodSystem : CloningPodSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private EuiManager _euiManager = null!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MaterialStorageSystem _material = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<EmaggedComponent> _emaggedQuery;

    private readonly ProtoId<ReagentPrototype> _bloodId = "Blood";

    /// <inheritdoc/>
    protected override void OpenEui(Entity<MindComponent> mindEnt, MindComponent mind, ICommonSession client)
    {
        _euiManager.OpenEui(new AcceptCloningEui(mindEnt, mind, this), client);
    }

    /// <inheritdoc/>
    protected override void EndFailedCloning(Entity<CloningPodComponent> ent)
    {
        ent.Comp.FailedClone = false;
        ent.Comp.NextUpdate = TimeSpan.Zero;
        ent.Comp.PowerLostAt = null;
        UpdateStatus(ent, CloningPodStatus.Idle);
        var transform = Transform(ent.Owner);
        var indices = _transform.GetGridTilePositionOrDefault((ent.Owner, transform));
        var tileMix = _atmosphere.GetTileMixture(transform.GridUid, null, indices, true);

        if (_emaggedQuery.HasComp(ent.Owner))
        {
            _audio.PlayPvs(ent.Comp.ScreamSound, ent.Owner);
            Spawn(ent.Comp.MobSpawnId, transform.Coordinates);
        }

        Solution bloodSolution = new();

        var i = 0;
        while (i < 1)
        {
            tileMix?.AdjustMoles(Gas.Ammonia, 6f);
            bloodSolution.AddReagent(_bloodId, 50);
            if (_random.Prob(0.2f))
                i++;
        }

        _puddle.TrySpillAt(ent.Owner, bloodSolution, out _);

        if (!_emaggedQuery.HasComp(ent.Owner))
            _material.SpawnMultipleFromMaterial(_random.Next(1, (int)(ent.Comp.UsedBiomass / 2.5)), ent.Comp.RequiredMaterial, Transform(ent.Owner).Coordinates);

        ent.Comp.UsedBiomass = 0;
        RemCompDeferred<ActiveCloningPodComponent>(ent.Owner);
        Dirty(ent);
    }
}
