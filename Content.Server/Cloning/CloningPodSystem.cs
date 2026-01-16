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
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cloning;

public sealed class CloningPodSystem : SharedCloningPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly ProtoId<ReagentPrototype> _bloodId = "Blood";

    /// <inheritdoc/>
    protected override void OpenEui(Entity<MindComponent> mindEnt, MindComponent mind, ICommonSession client)
    {
        _euiManager.OpenEui(new AcceptCloningEui(mindEnt, mind, this), client);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveCloningPodComponent, CloningPodComponent>();
        while (query.MoveNext(out var uid, out var _, out var cloning))
        {
            if (!_powerReceiver.IsPowered(uid))
                continue;

            if (cloning.BodyContainer.ContainedEntity == null && !cloning.FailedClone)
                continue;

            cloning.NextUpdate += TimeSpan.FromSeconds(frameTime);
            if (cloning.NextUpdate < cloning.CloningTime)
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

        if (ent.Comp.BodyContainer.ContainedEntity is not { Valid: true } entity || ent.Comp.NextUpdate < ent.Comp.CloningTime)
            return;

        RemComp<BeingClonedComponent>(entity);
        _container.Remove(entity, ent.Comp.BodyContainer);
        ent.Comp.NextUpdate = TimeSpan.Zero;
        ent.Comp.UsedBiomass = 0;
        UpdateStatus((ent.Owner, ent.Comp), CloningPodStatus.Idle);
        RemCompDeferred<ActiveCloningPodComponent>(ent.Owner);
        Dirty(ent);
    }

    private void EndFailedCloning(Entity<CloningPodComponent> ent)
    {
        ent.Comp.FailedClone = false;
        ent.Comp.NextUpdate = TimeSpan.Zero;
        UpdateStatus(ent, CloningPodStatus.Idle);
        var transform = Transform(ent.Owner);
        var indices = _transform.GetGridTilePositionOrDefault((ent.Owner, transform));
        var tileMix = _atmosphere.GetTileMixture(transform.GridUid, null, indices, true);

        if (HasComp<EmaggedComponent>(ent.Owner))
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

        if (!HasComp<EmaggedComponent>(ent.Owner))
            _material.SpawnMultipleFromMaterial(_random.Next(1, (int)(ent.Comp.UsedBiomass / 2.5)), ent.Comp.RequiredMaterial, Transform(ent.Owner).Coordinates);

        ent.Comp.UsedBiomass = 0;
        RemCompDeferred<ActiveCloningPodComponent>(ent.Owner);
        Dirty(ent);
    }
}
