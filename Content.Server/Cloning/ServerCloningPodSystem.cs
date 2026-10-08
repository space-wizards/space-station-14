using Content.Server.Atmos.EntitySystems;
using Content.Server.EUI;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cloning;
using Content.Shared.Emag.Components;
using Content.Shared.Mind;
using Content.Shared.Power;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Cloning;

/// <inheritdoc/>
public sealed partial class ServerCloningPodSystem : CloningPodSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private EuiManager _euiManager = null!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MaterialStorageSystem _material = default!;
    [Dependency] private PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<EmaggedComponent> _emaggedQuery;
    [Dependency] private EntityQuery<ActiveCloningPodComponent> _activePodQuery;

    private readonly ProtoId<ReagentPrototype> _bloodId = "Blood";

    /// <inheritdoc/>
    protected override void OpenEui(Entity<MindComponent> mindEnt, MindComponent mind, ICommonSession client)
    {
        _euiManager.OpenEui(new AcceptCloningEui(mindEnt, mind, this), client);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CloningPodComponent> ent, ref PowerChangedEvent args)
    {
        if (!_activePodQuery.HasComp(ent.Owner))
            return;

        UpdatePowerTimer(ent, args.Powered, _timing.CurTime);
    }

    private void UpdatePowerTimer(Entity<CloningPodComponent> ent, bool powered, TimeSpan curTime)
    {
        if (!powered)
        {
            ent.Comp.PowerLostAt ??= curTime;
            return;
        }

        if (ent.Comp.PowerLostAt is not { } powerLostAt)
            return;

        ent.Comp.NextUpdate += curTime - powerLostAt;
        ent.Comp.PowerLostAt = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCloningPodComponent, CloningPodComponent>();
        while (query.MoveNext(out var uid, out var _, out var cloning))
        {
            var powered = _powerReceiver.IsPowered(uid);
            UpdatePowerTimer((uid, cloning), powered, curTime);
            if (!powered)
                continue;

            if (cloning.BodyContainer?.ContainedEntity == null && !cloning.FailedClone)
                continue;

            if (curTime < cloning.NextUpdate)
                continue;

            if (cloning.FailedClone)
                EndFailedCloning((uid, cloning));
            else
                Eject((uid, cloning));
        }
    }

    public void Eject(Entity<CloningPodComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.BodyContainer?.ContainedEntity is not { Valid: true } entity || _timing.CurTime < ent.Comp.NextUpdate)
            return;

        RemComp<BeingClonedComponent>(entity);
        _container.Remove(entity, ent.Comp.BodyContainer);
        ent.Comp.NextUpdate = TimeSpan.Zero;
        ent.Comp.PowerLostAt = null;
        ent.Comp.UsedBiomass = 0;
        UpdateStatus((ent.Owner, ent.Comp), CloningPodStatus.Idle);
        RemCompDeferred<ActiveCloningPodComponent>(ent.Owner);
        Dirty(ent);
    }

    private void EndFailedCloning(Entity<CloningPodComponent> ent)
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
