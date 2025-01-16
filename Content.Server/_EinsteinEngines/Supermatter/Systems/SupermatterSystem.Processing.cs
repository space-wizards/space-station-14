using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Sound.Components;
using Content.Shared._EinsteinEngines.CCVar;
using Content.Shared._EinsteinEngines.Supermatter.Components;
using Content.Shared._EinsteinEngines.Supermatter.Monitor;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Radiation.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Speech;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server._EinsteinEngines.Supermatter.Systems;

public sealed partial class SupermatterSystem
{
    /// <summary>
    /// Handle power and radiation output depending on atmospheric things.
    /// </summary>
    private void ProcessAtmos(EntityUid uid, SupermatterComponent sm)
    {
        var mix = _atmosphere.GetContainingMixture(uid, true, true);

        if (mix is not { })
            return;

        sm.GasStorage = mix.Remove(sm.GasEfficiency * mix.TotalMoles);
        var moles = sm.GasStorage.TotalMoles;

        if (!(moles > 0f))
            return;

        var gasComposition = sm.GasStorage.Clone();

        // Let's get the proportions of the gases in the mix for scaling stuff later
        // They range between 0 and 1
        foreach (var gasId in Enum.GetValues<Gas>())
        {
            var proportion = sm.GasStorage.GetMoles(gasId) / moles;
            gasComposition.SetMoles(gasId, Math.Clamp(proportion, 0, 1));
        }

        // No less then zero, and no greater then one, we use this to do explosions and heat to power transfer.
        var powerRatio = SupermatterGasData.GetPowerMixRatios(gasComposition);

        // Affects plasma, o2 and heat output.
        sm.HeatModifier = SupermatterGasData.GetHeatPenalties(gasComposition);
        var transmissionBonus = SupermatterGasData.GetTransmitModifiers(gasComposition);

        var h2OBonus = 1 - gasComposition.GetMoles(Gas.WaterVapor) * 0.25f;

        powerRatio = Math.Clamp(powerRatio, 0, 1);
        sm.HeatModifier = Math.Max(sm.HeatModifier, 0.5f);
        transmissionBonus *= h2OBonus;

        // Affects the damage heat does to the crystal
        var heatResistance = SupermatterGasData.GetHeatResistances(gasComposition);
        sm.DynamicHeatResistance = Math.Max(heatResistance, 1);

        // More moles of gases are harder to heat than fewer, so let's scale heat damage around them
        sm.MoleHeatPenaltyThreshold = (float)Math.Max(moles / sm.MoleHeatPenalty, 0.25);

        // Ramps up or down in increments of 0.02 up to the proportion of CO2
        // Given infinite time, powerloss_dynamic_scaling = co2comp
        // Some value from 0-1
        if (moles > sm.PowerlossInhibitionMoleThreshold && gasComposition.GetMoles(Gas.CarbonDioxide) > sm.PowerlossInhibitionGasThreshold)
        {
            var co2powerloss = Math.Clamp(gasComposition.GetMoles(Gas.CarbonDioxide) - sm.PowerlossDynamicScaling, -0.02f, 0.02f);
            sm.PowerlossDynamicScaling = Math.Clamp(sm.PowerlossDynamicScaling + co2powerloss, 0f, 1f);
        }
        else
            sm.PowerlossDynamicScaling = Math.Clamp(sm.PowerlossDynamicScaling - 0.05f, 0f, 1f);

        // Ranges from 0~1(1 - (0~1 * 1~(1.5 * (mol / 500))))
        // We take the mol count, and scale it to be our inhibitor
        sm.PowerlossInhibitor = Math.Clamp(1 - sm.PowerlossDynamicScaling * Math.Clamp(moles / sm.PowerlossInhibitionMoleBoostThreshold, 1f, 1.5f), 0f, 1f);

        if (sm.MatterPower != 0)
        {
            // We base our removed power off 1/10 the matter_power.
            var removedMatter = Math.Max(sm.MatterPower / sm.MatterPowerConversion, 40);
            // Adds at least 40 power
            sm.Power = Math.Max(sm.Power + removedMatter, 0);
            // Removes at least 40 matter power
            sm.MatterPower = Math.Max(sm.MatterPower - removedMatter, 0);
        }

        // Based on gas mix, makes the power more based on heat or less effected by heat
        var tempFactor = powerRatio > 0.8 ? 50f : 30f;

        // If there is more frezon and N2 then anything else, we receive no power increase from heat
        sm.Power = Math.Max((sm.GasStorage.Temperature * tempFactor / Atmospherics.T0C) * powerRatio + sm.Power, 0);

        // Irradiate stuff
        if (TryComp<RadiationSourceComponent>(uid, out var rad))
        {
            rad.Intensity =
                _config.GetCVar(EinsteinCCVars.SupermatterRadsBase) +
                (sm.Power
                * Math.Max(0, 1f + transmissionBonus / 10f)
                * 0.003f
                * _config.GetCVar(EinsteinCCVars.SupermatterRadsModifier));

            rad.Slope = Math.Clamp(rad.Intensity / 15, 0.2f, 1f);
        }

        // Power * 0.55
        var energy = sm.Power * sm.ReactionPowerModifier;

        // Keep in mind we are only adding this temperature to (efficiency)% of the one tile the rock is on.
        // An increase of 4°C at 25% efficiency here results in an increase of 1°C / (#tilesincore) overall.
        // Power * 0.55 * 1.5~23 / 5
        sm.GasStorage.Temperature += (energy * sm.HeatModifier) / sm.ThermalReleaseModifier;
        sm.GasStorage.Temperature = Math.Max(0,
            Math.Min(sm.GasStorage.Temperature, 2500f * sm.HeatModifier));

        // Release the waste
        sm.GasStorage.AdjustMoles(Gas.Plasma, Math.Max(energy * sm.HeatModifier / sm.PlasmaReleaseModifier, 0f));
        sm.GasStorage.AdjustMoles(Gas.Oxygen, Math.Max((energy + sm.GasStorage.Temperature * sm.HeatModifier - Atmospherics.T0C) / sm.OxygenReleaseEfficiencyModifier, 0f));

        _atmosphere.Merge(mix, sm.GasStorage);

        var powerReduction = (float) Math.Pow(sm.Power / 500, 3);

        // After this point power is lowered
        // This wraps around to the begining of the function
        sm.Power = Math.Max(sm.Power - Math.Min(powerReduction * sm.PowerlossInhibitor, sm.Power * 0.83f * sm.PowerlossInhibitor), 0f);
    }

    /// <summary>
    /// Shoot lightning bolts depending on accumulated power.
    /// </summary>
    private void SupermatterZap(EntityUid uid, SupermatterComponent sm)
    {
        var zapPower = 0;
        var zapCount = 0;
        var zapRange = Math.Clamp(sm.Power / 1000, 2, 7);

        if (_random.Prob(0.05f))
            zapCount += 1;

        if (sm.Power >= sm.PowerPenaltyThreshold)
            zapCount += 2;

        if (sm.Power >= sm.SeverePowerPenaltyThreshold)
        {
            zapPower += 1;
            zapCount += 1;
        }

        if (sm.Power >= sm.CriticalPowerPenaltyThreshold)
        {
            zapPower += 1;
            zapCount += 1;
        }

        if (zapCount >= 1)
            _lightning.ShootRandomLightnings(uid, zapRange, zapCount, sm.LightningPrototypes[zapPower], hitCoordsChance: sm.ZapHitCoordinatesChance);
    }

    /// <summary>
    /// Generate temporary anomalies depending on accumulated power.
    /// </summary>
    private void GenerateAnomalies(EntityUid uid, SupermatterComponent sm)
    {
        var xform = Transform(uid);
        var anomalies = new List<string>();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        // Bluespace anomaly: ~1/150 chance
        if (_random.Prob(1 / sm.AnomalyBluespaceChance))
            anomalies.Add(sm.AnomalyBluespaceSpawnPrototype);

        // Gravity anomaly: ~1/150 chance above SeverePowerPenaltyThreshold, or ~1/750 chance otherwise
        if (sm.Power > sm.SeverePowerPenaltyThreshold && _random.Prob(1 / sm.AnomalyGravityChanceSevere) ||
            _random.Prob(1 / sm.AnomalyGravityChance))
            anomalies.Add(sm.AnomalyGravitySpawnPrototype);

        // Pyroclastic anomaly: ~1/375 chance above SeverePowerPenaltyThreshold, or ~1/2500 chance above PowerPenaltyThreshold
        if (sm.Power > sm.SeverePowerPenaltyThreshold && _random.Prob(1 / sm.AnomalyPyroChanceSevere) ||
            sm.Power > sm.PowerPenaltyThreshold && _random.Prob(1 / sm.AnomalyPyroChance))
            anomalies.Add(sm.AnomalyPyroSpawnPrototype);

        var count = anomalies.Count;
        if (count == 0)
            return;

        var tiles = GetSpawningPoints(uid, sm, count);
        if (tiles == null)
            return;

        foreach (var tileref in tiles)
        {
            var anomaly = Spawn(_random.Pick(anomalies), _map.ToCenterCoordinates(tileref, grid));
            EnsureComp<TimedDespawnComponent>(anomaly).Lifetime = sm.AnomalyLifetime;
        }
    }

    /// <summary>
    /// Gets random points around the supermatter.
    /// Most of this is from GetSpawningPoints() in SharedAnomalySystem
    /// </summary>
    private List<TileRef>? GetSpawningPoints(EntityUid uid, SupermatterComponent sm, int amount)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        var localpos = xform.Coordinates.Position;
        var tilerefs = _map.GetLocalTilesIntersecting(
            xform.GridUid.Value,
            grid,
            new Box2(localpos + new Vector2(-sm.AnomalySpawnMaxRange, -sm.AnomalySpawnMaxRange), localpos + new Vector2(sm.AnomalySpawnMaxRange, sm.AnomalySpawnMaxRange)))
            .ToList();

        if (tilerefs.Count == 0)
            return null;

        var physQuery = GetEntityQuery<PhysicsComponent>();
        var resultList = new List<TileRef>();
        while (resultList.Count < amount)
        {
            if (tilerefs.Count == 0)
                break;

            var tileref = _random.Pick(tilerefs);
            var distance = MathF.Sqrt(MathF.Pow(tileref.X - xform.LocalPosition.X, 2) + MathF.Pow(tileref.Y - xform.LocalPosition.Y, 2));

            // Cut outer & inner circle
            if (distance > sm.AnomalySpawnMaxRange || distance < sm.AnomalySpawnMinRange)
            {
                tilerefs.Remove(tileref);
                continue;
            }

            var valid = true;

            // TODO: obsolete function
            foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;

                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int)CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }

            if (!valid)
            {
                tilerefs.Remove(tileref);
                continue;
            }

            resultList.Add(tileref);
        }

        return resultList;
    }

    /// <summary>
    /// Handles environmental damage.
    /// </summary>
    private void HandleDamage(EntityUid uid, SupermatterComponent sm)
    {
        var xform = Transform(uid);
        var indices = _xform.GetGridOrMapTilePosition(uid, xform);

        sm.DamageArchived = sm.Damage;

        var mix = _atmosphere.GetContainingMixture(uid, true, true);

        // We're in space or there is no gas to process
        if (!xform.GridUid.HasValue || mix is not { } || mix.TotalMoles == 0f)
        {
            sm.Damage += Math.Max(sm.Power / 1000 * sm.DamageIncreaseMultiplier, 0.1f);
            return;
        }

        // Absorbed gas from surrounding area
        var absorbedGas = mix.Remove(sm.GasEfficiency * mix.TotalMoles);
        var moles = absorbedGas.TotalMoles;

        var totalDamage = 0f;

        var tempThreshold = Atmospherics.T0C + sm.HeatPenaltyThreshold;

        // Temperature start to have a positive effect on damage after 350
        var tempDamage = Math.Max(Math.Clamp(moles / 200f, .5f, 1f) * absorbedGas.Temperature - tempThreshold * sm.DynamicHeatResistance, 0f) *
            sm.MoleHeatPenaltyThreshold / 150f * sm.DamageIncreaseMultiplier;
        totalDamage += tempDamage;

        // Power only starts affecting damage when it is above 5000
        var powerDamage = Math.Max(sm.Power - sm.PowerPenaltyThreshold, 0f) / 500f * sm.DamageIncreaseMultiplier;
        totalDamage += powerDamage;

        // Mol count only starts affecting damage when it is above 1800
        var moleDamage = Math.Max(moles - sm.MolePenaltyThreshold, 0f) / 80 * sm.DamageIncreaseMultiplier;
        totalDamage += moleDamage;

        // Healing damage
        if (moles < sm.MolePenaltyThreshold)
        {
            var healHeatDamage = Math.Min(absorbedGas.Temperature - tempThreshold, 0f) / 150;
            totalDamage += healHeatDamage;
        }

        // Check for space tiles next to SM
        //TODO: Change moles out for checking if adjacent tiles exist
        var enumerator = _atmosphere.GetAdjacentTileMixtures(xform.GridUid.Value, indices, false, false);
        while (enumerator.MoveNext(out var ind))
        {
            if (ind.TotalMoles != 0)
                continue;

            var integrity = GetIntegrity(sm);

            var factor = integrity switch
            {
                < 10 => 0.0005f,
                < 25 => 0.0009f,
                < 45 => 0.005f,
                < 75 => 0.002f,
                _ => 0f
            };

            totalDamage += Math.Clamp(sm.Power * factor * sm.DamageIncreaseMultiplier, 0f, sm.MaxSpaceExposureDamage);

            break;
        }

        var damage = Math.Min(sm.DamageArchived + sm.DamageHardcap * sm.DamageDelaminationPoint, sm.Damage + totalDamage);

        // Prevent it from going negative
        sm.Damage = Math.Clamp(damage, 0, float.PositiveInfinity);
    }

    /// <summary>
    /// Handles core damage announcements
    /// </summary>
    private void AnnounceCoreDamage(EntityUid uid, SupermatterComponent sm)
    {
        // If undamaged, no need to announce anything
        if (sm.Damage == 0)
            return;

        var message = string.Empty;
        var global = false;

        var integrity = GetIntegrity(sm).ToString("0.00");

        // Instantly announce delamination
        if (sm.Delamming && !sm.DelamAnnounced)
        {
            var sb = new StringBuilder();
            var loc = string.Empty;

            switch (sm.PreferredDelamType)
            {
                case DelamType.Cascade: loc = "supermatter-delam-cascade";   break;
                case DelamType.Singulo: loc = "supermatter-delam-overmass";  break;
                case DelamType.Tesla:   loc = "supermatter-delam-tesla";     break;
                default:                loc = "supermatter-delam-explosion"; break;
            }

            sb.AppendLine(Loc.GetString(loc));
            sb.Append(Loc.GetString("supermatter-seconds-before-delam", ("seconds", sm.DelamTimer)));

            message = sb.ToString();
            global = true;
            sm.DelamAnnounced = true;
            sm.YellTimer = TimeSpan.FromSeconds(sm.DelamTimer / 2);

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        // Only announce every YellTimer seconds
        if (_timing.CurTime < sm.YellLast + sm.YellTimer)
            return;

        // Recovered after the delamination point
        if (sm.Damage < sm.DamageDelaminationPoint && sm.DelamAnnounced)
        {
            message = Loc.GetString("supermatter-delam-cancel", ("integrity", integrity));
            sm.DelamAnnounced = false;
            sm.YellTimer = TimeSpan.FromSeconds(_config.GetCVar(EinsteinCCVars.SupermatterYellTimer));
            global = true;

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        // Oh god oh fuck
        if (sm.Delamming && sm.DelamAnnounced)
        {
            var seconds = Math.Ceiling(sm.DelamEndTime.TotalSeconds - _timing.CurTime.TotalSeconds);

            if (seconds <= 0)
                return;

            var loc = seconds switch
            {
                >  5 => "supermatter-seconds-before-delam-countdown",
                <= 5 => "supermatter-seconds-before-delam-imminent",
                _ => String.Empty
            };

            sm.YellTimer = seconds switch
            {
                > 30 => TimeSpan.FromSeconds(10),
                >  5 => TimeSpan.FromSeconds(5),
                <= 5 => TimeSpan.FromSeconds(1),
                _ => TimeSpan.FromSeconds(_config.GetCVar(EinsteinCCVars.SupermatterYellTimer))
            };

            if (seconds <= 5 && TryComp<SpeechComponent>(uid, out var speech))
                // Prevent repeat sounds during the 5.. 4.. 3.. 2.. 1.. countdown
                speech.SoundCooldownTime = 5.0f;

            message = Loc.GetString(loc, ("seconds", seconds));
            global = true;

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        // We're safe
        if (sm.Damage < sm.DamageArchived && sm.Status >= SupermatterStatusType.Warning)
        {
            message = Loc.GetString("supermatter-healing", ("integrity", integrity));

            if (sm.Status >= SupermatterStatusType.Emergency)
                global = true;

            if (TryComp<SpeechComponent>(uid, out var speech))
                // Reset speech cooldown after healing is started
                speech.SoundCooldownTime = 0.0f;

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        // Ignore the 0% integrity alarm
        if (sm.Delamming)
            return;

        // We are not taking consistent damage, Engineers aren't needed
        if (sm.Damage <= sm.DamageArchived)
            return;

        // Announce damage and any dangerous thresholds
        if (sm.Damage >= sm.DamageWarningThreshold)
        {
            message = Loc.GetString("supermatter-warning", ("integrity", integrity));
            if (sm.Damage >= sm.DamageEmergencyThreshold)
            {
                message = Loc.GetString("supermatter-emergency", ("integrity", integrity));
                global = true;
            }

            SendSupermatterAnnouncement(uid, sm, message, global);

            global = false;

            if (sm.Power >= sm.PowerPenaltyThreshold)
            {
                message = Loc.GetString("supermatter-threshold-power");
                SendSupermatterAnnouncement(uid, sm, message, global);

                if (sm.PowerlossInhibitor < 0.5)
                {
                    message = Loc.GetString("supermatter-threshold-powerloss");
                    SendSupermatterAnnouncement(uid, sm, message, global);
                }
            }

            if (sm.GasStorage != null && sm.GasStorage.TotalMoles >= sm.MolePenaltyThreshold)
            {
                message = Loc.GetString("supermatter-threshold-mole");
                SendSupermatterAnnouncement(uid, sm, message, global);
            }
        }
    }

    /// <summary>
    /// Sends the given message to local chat and a radio channel
    /// </summary>
    /// <param name="global">If true, sends the message to the common radio</param>
    public void SendSupermatterAnnouncement(EntityUid uid, SupermatterComponent sm, string message, bool global = false)
    {
        if (message == String.Empty)
            return;

        var channel = sm.Channel;

        if (global)
            channel = sm.ChannelGlobal;

        // Ensure status, otherwise the wrong speech sound may be used
        HandleStatus(uid, sm);

        sm.YellLast = _timing.CurTime;
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false, checkRadioPrefix: true);
        _radio.SendRadioMessage(uid, message, channel, uid);
    }

    /// <summary>
    /// Returns the integrity rounded to hundreds, e.g. 100.00%
    /// </summary>
    public static float GetIntegrity(SupermatterComponent sm)
    {
        var integrity = sm.Damage / sm.DamageDelaminationPoint;
        integrity = (float) Math.Round(100 - integrity * 100, 2);
        integrity = integrity < 0 ? 0 : integrity;
        return integrity;
    }

    /// <summary>
    /// Decide on how to delaminate.
    /// </summary>
    public DelamType ChooseDelamType(EntityUid uid, SupermatterComponent sm)
    {
        if (_config.GetCVar(EinsteinCCVars.SupermatterDoForceDelam))
            return _config.GetCVar(EinsteinCCVars.SupermatterForcedDelamType);

        var mix = _atmosphere.GetContainingMixture(uid, true, true);

        if (mix is { })
        {
            var absorbedGas = mix.Remove(sm.GasEfficiency * mix.TotalMoles);
            var moles = absorbedGas.TotalMoles;

            if (_config.GetCVar(EinsteinCCVars.SupermatterDoSingulooseDelam)
                && moles >= sm.MolePenaltyThreshold * _config.GetCVar(EinsteinCCVars.SupermatterSingulooseMolesModifier))
                return DelamType.Singulo;
        }

        if (_config.GetCVar(EinsteinCCVars.SupermatterDoTeslooseDelam)
            && sm.Power >= sm.PowerPenaltyThreshold * _config.GetCVar(EinsteinCCVars.SupermatterTesloosePowerModifier))
            return DelamType.Tesla;

        //TODO: Add resonance cascade when there's crazy conditions or a destabilizing crystal

        return DelamType.Explosion;
    }

    /// <summary>
    /// Handle the end of the station.
    /// </summary>
    private void HandleDelamination(EntityUid uid, SupermatterComponent sm)
    {
        var xform = Transform(uid);

        sm.PreferredDelamType = ChooseDelamType(uid, sm);

        if (!sm.Delamming)
        {
            sm.Delamming = true;
            sm.DelamEndTime = _timing.CurTime + TimeSpan.FromSeconds(sm.DelamTimer);
            AnnounceCoreDamage(uid, sm);
        }

        if (sm.Damage < sm.DamageDelaminationPoint && sm.Delamming)
        {
            sm.Delamming = false;
            AnnounceCoreDamage(uid, sm);
        }

        if (_timing.CurTime < sm.DelamEndTime)
            return;

        var mapId = Transform(uid).MapID;
        var mapFilter = Filter.BroadcastMap(mapId);
        var message = Loc.GetString("supermatter-delam-player");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        // Send the reality distortion message to every player on the map
        _chatManager.ChatMessageToManyFiltered(mapFilter,
            ChatChannel.Server,
            message,
            wrappedMessage,
            uid,
            false,
            true,
            Color.Red);

        // Play the reality distortion sound for every player on the map
        _audio.PlayGlobal(sm.DistortSound, mapFilter, true);

        // Add hallucinations to every player on the map
        // TODO: change this from paracusia to actual hallucinations whenever those are real
        var mobLookup = new HashSet<Entity<MobStateComponent>>();
        _entityLookup.GetEntitiesOnMap<MobStateComponent>(mapId, mobLookup);

        // These values match the paracusia disability, since we can't double up on paracusia
        var paracusiaSounds = new SoundCollectionSpecifier("Paracusia");
        var paracusiaMinTime = 0.1f;
        var paracusiaMaxTime = 300f;
        var paracusiaDistance = 7f;

        foreach (var mob in mobLookup)
        {
            // Ignore silicons
            if (HasComp<SiliconLawBoundComponent>(uid))
                continue;

            if (!EnsureComp<ParacusiaComponent>(mob, out var paracusia))
            {
                _paracusia.SetSounds(mob, paracusiaSounds, paracusia);
                _paracusia.SetTime(mob, paracusiaMinTime, paracusiaMaxTime, paracusia);
                _paracusia.SetDistance(mob, paracusiaDistance, paracusia);
            }
        }

        switch (sm.PreferredDelamType)
        {
            case DelamType.Cascade:
                Spawn(sm.KudzuSpawnPrototype, xform.Coordinates);
                break;

            case DelamType.Singulo:
                Spawn(sm.SingularitySpawnPrototype, xform.Coordinates);
                break;

            case DelamType.Tesla:
                Spawn(sm.TeslaSpawnPrototype, xform.Coordinates);
                break;

            default:
                _explosion.TriggerExplosive(uid);
                break;
        }
    }

    /// <summary>
    /// Sets the supermatter's status and speech sound based on thresholds
    /// </summary>
    private void HandleStatus(EntityUid uid, SupermatterComponent sm)
    {
        var currentStatus = GetStatus(uid, sm);

        if (sm.Status != currentStatus)
        {
            sm.Status = currentStatus;

            if (!TryComp<SpeechComponent>(uid, out var speech))
                return;

            sm.StatusCurrentSound = currentStatus switch
            {
                SupermatterStatusType.Warning => sm.StatusWarningSound,
                SupermatterStatusType.Danger => sm.StatusDangerSound,
                SupermatterStatusType.Emergency => sm.StatusEmergencySound,
                SupermatterStatusType.Delaminating => sm.StatusDelamSound,
                _ => default
            };

            if (currentStatus == SupermatterStatusType.Warning)
                speech.AudioParams = AudioParams.Default.AddVolume(7.5f);
            else
                speech.AudioParams = AudioParams.Default.AddVolume(10f);

            speech.SpeechSounds = sm.StatusCurrentSound;
        }

        // Supermatter is healing, don't play any speech sounds
        if (sm.Damage < sm.DamageArchived)
        {
            if (!TryComp<SpeechComponent>(uid, out var speech))
                return;

            sm.StatusCurrentSound = null;
            speech.SpeechSounds = null;
        }
    }

    // This currently has some audio clipping issues: this is likely an issue with AmbientSoundComponent or the engine
    /// <summary>
    /// Swaps out ambience sounds when the SM is delamming or not.
    /// </summary>
    private void HandleSoundLoop(EntityUid uid, SupermatterComponent sm)
    {
        if (!TryComp<AmbientSoundComponent>(uid, out var ambient))
            return;

        var volume = (float) Math.Round(Math.Clamp((sm.Power / 50) - 5, -5, 5));

        _ambient.SetVolume(uid, volume);

        if (sm.Status >= SupermatterStatusType.Danger && sm.CurrentSoundLoop != sm.DelamLoopSound)
            sm.CurrentSoundLoop = sm.DelamLoopSound;

        else if (sm.Status < SupermatterStatusType.Danger && sm.CurrentSoundLoop != sm.CalmLoopSound)
            sm.CurrentSoundLoop = sm.CalmLoopSound;

        if (ambient.Sound != sm.CurrentSoundLoop)
            _ambient.SetSound(uid, sm.CurrentSoundLoop, ambient);
    }

    /// <summary>
    /// Plays normal/delam sounds at a rate determined by power and damage
    /// </summary>
    private void HandleAccent(EntityUid uid, SupermatterComponent sm)
    {
        var emit = Comp<EmitSoundOnTriggerComponent>(uid);

        if (emit == null)
            return;

        if (sm.AccentLastTime >= _timing.CurTime || !_random.Prob(0.05f))
            return;

        var aggression = Math.Min((sm.Damage / 800) * (sm.Power / 2500), 1) * 100;
        var nextSound = Math.Max(Math.Round((100 - aggression) * 5), sm.AccentMinCooldown);

        if (sm.AccentLastTime + TimeSpan.FromSeconds(nextSound) > _timing.CurTime)
            return;

        if (sm.Status >= SupermatterStatusType.Danger && emit.Sound != sm.DelamAccent)
            emit.Sound = sm.DelamAccent;

        else if (sm.Status < SupermatterStatusType.Danger && emit.Sound != sm.CalmAccent)
            emit.Sound = sm.CalmAccent;

        sm.AccentLastTime = _timing.CurTime;

        var ev = new TriggerEvent(uid);
        RaiseLocalEvent(uid, ev);
    }
}
