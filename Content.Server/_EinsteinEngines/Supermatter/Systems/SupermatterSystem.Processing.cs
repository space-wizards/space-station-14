using System.Linq;
using System.Text;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Sound.Components;
using Content.Shared._EinsteinEngines.Supermatter.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Popups;
using Content.Shared.Radiation.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._EinsteinEngines.Supermatter.Systems;

public sealed partial class SupermatterSystem
{
    /// <summary>
    ///     Handle power and radiation output depending on atmospheric things.
    /// </summary>
    private void ProcessAtmos(EntityUid uid, SupermatterComponent sm)
    {
        var mix = _atmosphere.GetContainingMixture(uid, true, true);

        if (mix is not { })
            return;

        var absorbedGas = mix.Remove(sm.GasEfficiency * mix.TotalMoles);
        var moles = absorbedGas.TotalMoles;

        if (!(moles > 0f))
            return;

        var gases = sm.GasStorage;
        var facts = sm.GasDataFields;

        // Lets get the proportions of the gasses in the mix for scaling stuff later
        // They range between 0 and 1
        gases = gases.ToDictionary(
            gas => gas.Key,
            gas => Math.Clamp(absorbedGas.GetMoles(gas.Key) / moles, 0, 1)
        );

        // No less then zero, and no greater then one, we use this to do explosions and heat to power transfer.
        var powerRatio = gases.Sum(gas => gases[gas.Key] * facts[gas.Key].PowerMixRatio);

        // Minimum value of -10, maximum value of 23. Affects plasma, o2 and heat output.
        var heatModifier = gases.Sum(gas => gases[gas.Key] * facts[gas.Key].HeatPenalty);

        // Minimum value of -10, maximum value of 23. Affects plasma, o2 and heat output.
        var transmissionBonus = gases.Sum(gas => gases[gas.Key] * facts[gas.Key].TransmitModifier);

        var h2OBonus = 1 - gases[Gas.WaterVapor] * 0.25f;

        powerRatio = Math.Clamp(powerRatio, 0, 1);
        heatModifier = Math.Max(heatModifier, 0.5f);
        transmissionBonus *= h2OBonus;

        // Effects the damage heat does to the crystal
        sm.DynamicHeatResistance = 1f;

        // More moles of gases are harder to heat than fewer, so let's scale heat damage around them
        sm.MoleHeatPenaltyThreshold = (float) Math.Max(moles * sm.MoleHeatPenalty, 0.25);

        // Ramps up or down in increments of 0.02 up to the proportion of CO2
        // Given infinite time, powerloss_dynamic_scaling = co2comp
        // Some value from 0-1
        if (moles > sm.PowerlossInhibitionMoleThreshold && gases[Gas.CarbonDioxide] > sm.PowerlossInhibitionGasThreshold)
        {
            var co2powerloss = Math.Clamp(gases[Gas.CarbonDioxide] - sm.PowerlossDynamicScaling, -0.02f, 0.02f);
            sm.PowerlossDynamicScaling = Math.Clamp(sm.PowerlossDynamicScaling + co2powerloss, 0f, 1f);
        }
        else
            sm.PowerlossDynamicScaling = Math.Clamp(sm.PowerlossDynamicScaling - 0.05f, 0f, 1f);

        // Ranges from 0~1(1 - (0~1 * 1~(1.5 * (mol / 500))))
        // We take the mol count, and scale it to be our inhibitor
        var powerlossInhibitor =
            Math.Clamp(
                1
                - sm.PowerlossDynamicScaling
                * Math.Clamp(
                    moles / sm.PowerlossInhibitionMoleBoostThreshold,
                    1f, 1.5f),
                0f, 1f);

        if (sm.MatterPower != 0) // We base our removed power off 1/10 the matter_power.
        {
            var removedMatter = Math.Max(sm.MatterPower / sm.MatterPowerConversion, 40);
            // Adds at least 40 power
            sm.Power = Math.Max(sm.Power + removedMatter, 0);
            // Removes at least 40 matter power
            sm.MatterPower = Math.Max(sm.MatterPower - removedMatter, 0);
        }

        // Based on gas mix, makes the power more based on heat or less effected by heat
        var tempFactor = powerRatio > 0.8 ? 50f : 30f;

        // If there is more pluox and N2 then anything else, we receive no power increase from heat
        sm.Power = Math.Max(absorbedGas.Temperature * tempFactor / Atmospherics.T0C * powerRatio + sm.Power, 0);

        // Irradiate stuff
        if (TryComp<RadiationSourceComponent>(uid, out var rad))
        {
            rad.Intensity =
                _config.GetCVar(CCVars.SupermatterRadsBase) +
                (sm.Power
                * Math.Max(0, 1f + transmissionBonus / 10f)
                * 0.003f
                * _config.GetCVar(CCVars.SupermatterRadsModifier));

            rad.Slope = Math.Clamp(rad.Intensity / 15, 0.2f, 1f);
        }

        // Power * 0.55 * 0.8~1
        var energy = sm.Power * sm.ReactionPowerModifier;

        // Keep in mind we are only adding this temperature to (efficiency)% of the one tile the rock is on.
        // An increase of 4°C at 25% efficiency here results in an increase of 1°C / (#tilesincore) overall.
        // Power * 0.55 * 1.5~23 / 5
        absorbedGas.Temperature += energy * heatModifier * sm.ThermalReleaseModifier;
        absorbedGas.Temperature = Math.Max(0,
            Math.Min(absorbedGas.Temperature, sm.HeatThreshold * heatModifier));

        // Release the waste
        absorbedGas.AdjustMoles(Gas.Plasma, Math.Max(energy * heatModifier * sm.PlasmaReleaseModifier, 0f));
        absorbedGas.AdjustMoles(Gas.Oxygen, Math.Max((energy + absorbedGas.Temperature * heatModifier - Atmospherics.T0C) * sm.OxygenReleaseEfficiencyModifier, 0f));

        _atmosphere.Merge(mix, absorbedGas);

        var powerReduction = (float) Math.Pow(sm.Power / 500, 3);

        // After this point power is lowered
        // This wraps around to the begining of the function
        sm.Power = Math.Max(sm.Power - Math.Min(powerReduction * powerlossInhibitor, sm.Power * 0.83f * powerlossInhibitor), 0f);
    }

    /// <summary>
    ///     Shoot lightning bolts depensing on accumulated power.
    /// </summary>
    private void SupermatterZap(EntityUid uid, SupermatterComponent sm)
    {
        var zapPower = 0;
        var zapCount = 0;
        var zapRange = Math.Clamp(sm.Power / 1000, 2, 7);

        // fuck this
        if (_random.Prob(0.05f))
        {
            zapCount += 1;
        }

        if (sm.Power >= sm.PowerPenaltyThreshold)
        {
            zapCount += 2;
        }

        if (sm.Power >= sm.SeverePowerPenaltyThreshold)
        {
            zapPower = 1;
            zapCount++;
        }

        if (sm.Power >= sm.CriticalPowerPenaltyThreshold)
        {
            zapPower = 2;
            zapCount++;
        }

        if (zapCount >= 1)
            _lightning.ShootRandomLightnings(uid, zapRange, zapCount, sm.LightningPrototypes[zapPower], hitCoordsChance: sm.ZapHitCoordinatesChance);
    }

    /// <summary>
    ///     Handles environmental damage.
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
        var tempDamage =
            Math.Max(
                Math.Clamp(moles / 200f, .5f, 1f)
                    * absorbedGas.Temperature
                    - tempThreshold
                    * sm.DynamicHeatResistance,
                0f)
                * sm.MoleHeatThreshold
                / 150f
                * sm.DamageIncreaseMultiplier;
        totalDamage += tempDamage;

        // Power only starts affecting damage when it is above 5000
        var powerDamage = Math.Max(sm.Power - sm.PowerPenaltyThreshold, 0f) / 500f * sm.DamageIncreaseMultiplier;
        totalDamage += powerDamage;

        // Mol count only starts affecting damage when it is above 1800
        var moleDamage = Math.Max(moles - sm.MolePenaltyThreshold, 0) / 80 * sm.DamageIncreaseMultiplier;
        totalDamage += moleDamage;

        // Healing damage
        if (moles < sm.MolePenaltyThreshold)
        {
            // There's a very small float so that it doesn't divide by 0
            var healHeatDamage = Math.Min(absorbedGas.Temperature - tempThreshold, 0.001f) / 150;
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

            totalDamage += Math.Clamp(sm.Power * factor * sm.DamageIncreaseMultiplier, 0, sm.MaxSpaceExposureDamage);

            break;
        }

        var damage = Math.Min(sm.DamageArchived + sm.DamageHardcap * sm.DamageDelaminationPoint, totalDamage);

        // Prevent it from going negative
        sm.Damage = Math.Clamp(damage, 0, float.PositiveInfinity);
    }

    /// <summary>
    ///     Handles core damage announcements
    /// </summary>
    private void AnnounceCoreDamage(EntityUid uid, SupermatterComponent sm)
    {
        // If undamaged, no need to announce anything
        if (sm.Damage == 0)
            return;

        var message = string.Empty;
        var global = false;

        var integrity = GetIntegrity(sm).ToString("0.00");

        // Special cases
        if (sm.Damage < sm.DamageDelaminationPoint && sm.DelamAnnounced)
        {
            message = Loc.GetString("supermatter-delam-cancel", ("integrity", integrity));
            sm.DelamAnnounced = false;
            sm.YellTimer = TimeSpan.FromSeconds(_config.GetCVar(CCVars.SupermatterYellTimer));
            global = true;
        }

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
                _ => TimeSpan.FromSeconds(_config.GetCVar(CCVars.SupermatterYellTimer))
            };

            message = Loc.GetString(loc, ("seconds", seconds));
            global = true;

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        // Ignore the 0% integrity alarm
        if (sm.Delamming)
            return;

        // We are not taking consistent damage, Engineers aren't needed
        if (sm.Damage <= sm.DamageArchived)
            return;

        if (sm.Damage >= sm.DamageWarningThreshold)
        {
            message = Loc.GetString("supermatter-warning", ("integrity", integrity));
            if (sm.Damage >= sm.DamageEmergencyThreshold)
            {
                message = Loc.GetString("supermatter-emergency", ("integrity", integrity));
                global = true;
            }
        }

        SendSupermatterAnnouncement(uid, sm, message, global);
    }

    /// <param name="global">If true, sends the message to the common radio</param>
    /// <param name="customSender">Localisation string for a custom announcer name</param>
    public void SendSupermatterAnnouncement(EntityUid uid, SupermatterComponent sm, string message, bool global = false)
    {
        if (message == String.Empty)
            return;

        var channel = sm.Channel;

        if (global)
            channel = sm.ChannelGlobal;

        sm.YellLast = _timing.CurTime;
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false, checkRadioPrefix: true);
        _radio.SendRadioMessage(uid, message, channel, uid);
    }

    /// <summary>
    ///     Returns the integrity rounded to hundreds, e.g. 100.00%
    /// </summary>
    public float GetIntegrity(SupermatterComponent sm)
    {
        var integrity = sm.Damage / sm.DamageDelaminationPoint;
        integrity = (float) Math.Round(100 - integrity * 100, 2);
        integrity = integrity < 0 ? 0 : integrity;
        return integrity;
    }

    /// <summary>
    ///     Decide on how to delaminate.
    /// </summary>
    public DelamType ChooseDelamType(EntityUid uid, SupermatterComponent sm)
    {
        if (_config.GetCVar(CCVars.SupermatterDoForceDelam))
            return _config.GetCVar(CCVars.SupermatterForcedDelamType);

        var mix = _atmosphere.GetContainingMixture(uid, true, true);

        if (mix is { })
        {
            var absorbedGas = mix.Remove(sm.GasEfficiency * mix.TotalMoles);
            var moles = absorbedGas.TotalMoles;

            if (_config.GetCVar(CCVars.SupermatterDoSingulooseDelam)
                && moles >= sm.MolePenaltyThreshold * _config.GetCVar(CCVars.SupermatterSingulooseMolesModifier))
                return DelamType.Singulo;
        }

        if (_config.GetCVar(CCVars.SupermatterDoTeslooseDelam)
            && sm.Power >= sm.PowerPenaltyThreshold * _config.GetCVar(CCVars.SupermatterTesloosePowerModifier))
            return DelamType.Tesla;

        //TODO: Add resonance cascade when there's crazy conditions or a destabilizing crystal

        return DelamType.Explosion;
    }

    /// <summary>
    ///     Handle the end of the station.
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

        var smTransform = Transform(uid);

        foreach (var pSession in Filter.GetAllPlayers())
        {
            var pEntity = pSession.AttachedEntity;

            if (pEntity != null
                && TryComp<TransformComponent>(pEntity, out var pTransform)
                && pTransform.MapID == smTransform.MapID)
                _popup.PopupEntity(Loc.GetString("supermatter-delam-player"), pEntity.Value, pEntity.Value, PopupType.MediumCaution);
        }
        
        _audio.PlayGlobal(sm.DistortSound, Filter.BroadcastMap(Transform(uid).MapID), true);

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
    ///     Swaps out ambience sounds when the SM is delamming or not.
    /// </summary>
    private void HandleSoundLoop(EntityUid uid, SupermatterComponent sm)
    {
        var ambient = Comp<AmbientSoundComponent>(uid);

        if (ambient == null)
            return;

        var volume = Math.Clamp((sm.Power / 50) - 5, -5, 5);

        _ambient.SetVolume(uid, volume);

        if (sm.Damage >= sm.DamageDelamAlertPoint && sm.CurrentSoundLoop != sm.DelamSound)
            sm.CurrentSoundLoop = sm.DelamSound;

        else if (sm.Damage < sm.DamageDelamAlertPoint && sm.CurrentSoundLoop != sm.CalmSound)
            sm.CurrentSoundLoop = sm.CalmSound;

        if (ambient.Sound != sm.CurrentSoundLoop)
            _ambient.SetSound(uid, sm.CurrentSoundLoop, ambient);
    }

    /// <summary>
    ///     Plays normal/delam sounds at a rate determined by power and damage
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

        if (sm.Damage >= sm.DamageDelamAlertPoint && emit.Sound != sm.DelamAccent)
            emit.Sound = sm.DelamAccent;

        else if (sm.Damage < sm.DamageDelamAlertPoint && emit.Sound != sm.CalmAccent)
            emit.Sound = sm.CalmAccent;

        sm.AccentLastTime = _timing.CurTime;

        var ev = new TriggerEvent(uid);
        RaiseLocalEvent(uid, ev);
    }
}
