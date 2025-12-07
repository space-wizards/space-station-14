using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cloning;
using Content.Shared.Damage.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cloning;

public sealed class CloningPodSystem : SharedCloningPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly ProtoId<CloningSettingsPrototype> _settingsId = "CloningPod";
    private readonly ProtoId<ReagentPrototype> _bloodId = "Blood";

    private const float EasyModeCloningCost = 0.7f;

    /// <inheritdoc/>
    public override bool TryCloning(Entity<CloningPodComponent?> ent, EntityUid bodyToClone, Entity<MindComponent> mindEnt, float failChanceModifier = 1)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (HasComp<ActiveCloningPodComponent>(ent.Owner))
            return false;

        var mind = mindEnt.Comp;
        if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
        {
            if (Exists(clone) &&
                !_mobState.IsDead(clone) &&
                TryComp<MindContainerComponent>(clone, out var cloneMindComp) &&
                (cloneMindComp.Mind == null || cloneMindComp.Mind == mindEnt))
                return false; // Mind already has clone.

            ClonesWaitingForMind.Remove(mind);
        }

        if (mind.OwnedEntity != null && !_mobState.IsDead(mind.OwnedEntity.Value))
            return false; // Body controlled by mind is not dead.

        // Yes, we still need to track down the client because we need to open the Eui.
        if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
            return false; // If we can't track down the client, we can't offer transfer. That'd be quite bad.

        if (!TryComp<PhysicsComponent>(bodyToClone, out var physics))
            return false;

        var cloningCost = (int)Math.Round(physics.FixturesMass);

        if (_configManager.GetCVar(CCVars.BiomassEasyMode))
            cloningCost = (int)Math.Round(cloningCost * EasyModeCloningCost);

        // biomass checks.
        var biomassAmount = _material.GetMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial);

        if (biomassAmount < cloningCost)
        {
            if (ent.Comp.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(ent.Comp.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-error", ("units", cloningCost)), InGameICChatType.Speak, false);
            return false;
        }
        // end of biomass checks.

        // genetic damage checks.
        if (TryComp<DamageableComponent>(bodyToClone, out var damageable) &&
            damageable.Damage.DamageDict.TryGetValue("Cellular", out var cellularDmg))
        {
            var chance = Math.Clamp((float)(cellularDmg / 100), 0, 1);
            chance *= failChanceModifier;

            if (cellularDmg > 0 && ent.Comp.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(ent.Comp.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - chance * 100))), InGameICChatType.Speak, false);

            if (_robustRandom.Prob(chance))
            {
                ent.Comp.FailedClone = true;
                UpdateStatus((ent.Owner, ent.Comp), CloningPodStatus.Gore);
                AddComp<ActiveCloningPodComponent>(ent.Owner);
                _material.TryChangeMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial, -cloningCost);
                ent.Comp.UsedBiomass = cloningCost;
                Dirty(ent);
                return true;
            }
        }
        // end of genetic damage checks.

        if (!_cloning.TryCloning(bodyToClone, _transform.GetMapCoordinates(bodyToClone), _settingsId, out var mob)) // spawn a new body
        {
            if (ent.Comp.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(ent.Comp.ConnectedConsole.Value, Loc.GetString("cloning-console-uncloneable-trait-error"), InGameICChatType.Speak, false);
            return false;
        }

        var cloneMindReturn = AddComp<BeingClonedComponent>(mob.Value);
        cloneMindReturn.Mind = mind;
        cloneMindReturn.Parent = ent.Owner;
        _container.Insert(mob.Value, ent.Comp.BodyContainer);
        ClonesWaitingForMind.Add(mind, mob.Value);
        _euiManager.OpenEui(new AcceptCloningEui(mindEnt, mind, this), client);

        UpdateStatus((ent.Owner, ent.Comp), CloningPodStatus.NoMind);
        AddComp<ActiveCloningPodComponent>(ent.Owner);
        _material.TryChangeMaterialAmount(ent.Owner, ent.Comp.RequiredMaterial, -cloningCost);
        ent.Comp.UsedBiomass = cloningCost;
        Dirty(ent);
        return true;
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
            if (_robustRandom.Prob(0.2f))
                i++;
        }
        _puddle.TrySpillAt(ent.Owner, bloodSolution, out _);

        if (!HasComp<EmaggedComponent>(ent.Owner))
            _material.SpawnMultipleFromMaterial(_robustRandom.Next(1, (int)(ent.Comp.UsedBiomass / 2.5)), ent.Comp.RequiredMaterial, Transform(ent.Owner).Coordinates);

        ent.Comp.UsedBiomass = 0;
        RemCompDeferred<ActiveCloningPodComponent>(ent.Owner);
        Dirty(ent);
    }
}
