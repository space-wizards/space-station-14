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
    public override bool TryCloning(EntityUid uid, EntityUid bodyToClone, Entity<MindComponent> mindEnt, CloningPodComponent? clonePod, float failChanceModifier = 1)
    {
        if (!Resolve(uid, ref clonePod))
            return false;

        if (HasComp<ActiveCloningPodComponent>(uid))
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
        var biomassAmount = _material.GetMaterialAmount(uid, clonePod.RequiredMaterial);

        if (biomassAmount < cloningCost)
        {
            if (clonePod.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-error", ("units", cloningCost)), InGameICChatType.Speak, false);
            return false;
        }
        // end of biomass checks.

        // genetic damage checks.
        if (TryComp<DamageableComponent>(bodyToClone, out var damageable) &&
            damageable.Damage.DamageDict.TryGetValue("Cellular", out var cellularDmg))
        {
            var chance = Math.Clamp((float)(cellularDmg / 100), 0, 1);
            chance *= failChanceModifier;

            if (cellularDmg > 0 && clonePod.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - chance * 100))), InGameICChatType.Speak, false);

            if (_robustRandom.Prob(chance))
            {
                clonePod.FailedClone = true;
                UpdateStatus(uid, CloningPodStatus.Gore, clonePod);
                AddComp<ActiveCloningPodComponent>(uid);
                _material.TryChangeMaterialAmount(uid, clonePod.RequiredMaterial, -cloningCost);
                clonePod.UsedBiomass = cloningCost;
                Dirty(uid, clonePod);
                return true;
            }
        }
        // end of genetic damage checks.

        if (!_cloning.TryCloning(bodyToClone, _transform.GetMapCoordinates(bodyToClone), _settingsId, out var mob)) // spawn a new body
        {
            if (clonePod.ConnectedConsole != null)
                _chat.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-uncloneable-trait-error"), InGameICChatType.Speak, false);
            return false;
        }

        var cloneMindReturn = AddComp<BeingClonedComponent>(mob.Value);
        cloneMindReturn.Mind = mind;
        cloneMindReturn.Parent = uid;
        _container.Insert(mob.Value, clonePod.BodyContainer);
        ClonesWaitingForMind.Add(mind, mob.Value);
        _euiManager.OpenEui(new AcceptCloningEui(mindEnt, mind, this), client);

        UpdateStatus(uid, CloningPodStatus.NoMind, clonePod);
        AddComp<ActiveCloningPodComponent>(uid);
        _material.TryChangeMaterialAmount(uid, clonePod.RequiredMaterial, -cloningCost);
        clonePod.UsedBiomass = cloningCost;
        Dirty(uid, clonePod);
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
                EndFailedCloning(uid, cloning);
            else
                Eject(uid, cloning);
        }
    }

    public void Eject(EntityUid uid, CloningPodComponent? clonePod)
    {
        if (!Resolve(uid, ref clonePod))
            return;

        if (clonePod.BodyContainer.ContainedEntity is not { Valid: true } entity || clonePod.NextUpdate < clonePod.CloningTime)
            return;

        RemComp<BeingClonedComponent>(entity);
        _container.Remove(entity, clonePod.BodyContainer);
        clonePod.NextUpdate = TimeSpan.Zero;
        clonePod.UsedBiomass = 0;
        UpdateStatus(uid, CloningPodStatus.Idle, clonePod);
        RemCompDeferred<ActiveCloningPodComponent>(uid);
        Dirty(uid, clonePod);
    }

    private void EndFailedCloning(EntityUid uid, CloningPodComponent clonePod)
    {
        clonePod.FailedClone = false;
        clonePod.NextUpdate = TimeSpan.Zero;
        UpdateStatus(uid, CloningPodStatus.Idle, clonePod);
        var transform = Transform(uid);
        var indices = _transform.GetGridTilePositionOrDefault((uid, transform));
        var tileMix = _atmosphere.GetTileMixture(transform.GridUid, null, indices, true);

        if (HasComp<EmaggedComponent>(uid))
        {
            _audio.PlayPvs(clonePod.ScreamSound, uid);
            Spawn(clonePod.MobSpawnId, transform.Coordinates);
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
        _puddle.TrySpillAt(uid, bloodSolution, out _);

        if (!HasComp<EmaggedComponent>(uid))
            _material.SpawnMultipleFromMaterial(_robustRandom.Next(1, (int)(clonePod.UsedBiomass / 2.5)), clonePod.RequiredMaterial, Transform(uid).Coordinates);

        clonePod.UsedBiomass = 0;
        RemCompDeferred<ActiveCloningPodComponent>(uid);
        Dirty(uid, clonePod);
    }
}
