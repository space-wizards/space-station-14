using Content.Server.Atmos.EntitySystems;
using Content.Server.Supermatter.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Atmos;
using Content.Server.Lightning;

using Consts = Content.Server.Supermatter.Components.SupermatterComponent; // using an alias for readability
using Robust.Shared.Audio.Systems;
using Content.Shared.Radiation.Components;
using Content.Server.Chat.Managers;
using Content.Shared.Interaction;
using Content.Server.Audio;
using Content.Shared.Audio;
using Robust.Shared.Random;
using Content.Server.Station.Systems;
using Content.Server.Station.Components;
using Content.Server.Anomaly;
using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Server.Popups;

namespace Content.Server.Supermatter.EntitySystems;

public sealed class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedAudioSystem _sound = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AmbientSoundSystem _ambience = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<SupermatterComponent, InteractUsingEvent>(OnClick);
        SubscribeLocalEvent<SupermatterComponent, SupermatterDoAfterEvent>(OnGetSliver);
        SubscribeLocalEvent<SupermatterComponent, DamageChangedEvent>(OnGetHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var sm in EntityQuery<SupermatterComponent>())
        {
            if (!sm.Activated)
                return;

            var uid = sm.Owner;

            sm.UpdateTimerAccumulator += frameTime;

            if (sm.UpdateTimerAccumulator >= sm.UpdateTimer)
            {
                sm.UpdateTimerAccumulator = 0f;

                ProcessAtmos(uid, sm);
                ProcessPower(uid, sm, frameTime);
                ProcessDamage(uid, sm);
                ProcessWaste(sm);

                HandleSound(uid, sm);
            }
        }
    }

    /// <summary>
    ///     Calculate power based on gases absorbed.
    /// </summary>
    private void ProcessAtmos(EntityUid uid, SupermatterComponent sm)
    {
        var mix = _atmos.GetTileMixture(uid);
        var absorbedMix = mix?.RemoveRatio(sm.AbsorptionRatio) ?? new();

        // calculate gases
        var gasPercentages = new float[Enum.GetValues(typeof(Gas)).Length];

        var powerTransmissionRate = 0f;
        var heatModifier = 0f;
        var heatResistance = 0f;
        var heatPowerGeneration = 0f;
        var powerlossInhibition = 0f;

        var moles = absorbedMix.TotalMoles;

        if (moles <= Consts.MinimumMoleCount)
            return;

        for (int i = 0; i < gasPercentages.Length; i++)
        {
            var moleCount = absorbedMix.GetMoles(i);

            if (moleCount <= Consts.MinimumMoleCount)
                continue;

            gasPercentages[i] = moleCount / moles;
            var smGas = sm.GasFacts[i];

            powerTransmissionRate += smGas.PowerTransmissionRate * gasPercentages[i];
            heatModifier += smGas.HeatModifier * gasPercentages[i];
            heatResistance += smGas.HeatResistance * gasPercentages[i];
            heatPowerGeneration += smGas.HeatPowerGeneration * gasPercentages[i];
            powerlossInhibition += smGas.PowerlossInhibition * gasPercentages[i];
        }

        heatPowerGeneration = Math.Clamp(heatPowerGeneration, 0, 1);
        powerlossInhibition = Math.Clamp(powerlossInhibition, 0, 1);

        // todo: add special gas effects, e.g. miasma getting fully consumed

        sm.AbsorbedGasMix = absorbedMix;
        sm.PowerTransmissionRate = powerTransmissionRate;
        sm.GasHeatModifier = heatModifier;
        sm.GasHeatResistance = heatResistance;
        sm.HeatPowerGeneration = heatPowerGeneration;
        sm.GasPowerlossInhibition = powerlossInhibition;
    }
    /// <summary>
    ///     Shoot lightning and radiate everything based on whatever power there is.
    /// </summary>
    private void ProcessPower(EntityUid uid, SupermatterComponent sm, float frameTime)
    {
        CalculateInternalEnergy(sm);
        CalculateZapRate(sm);

        _sound.PlayPvs(sm.SupermatterZapSound, uid);

        // very shitty and should not be working like that.
        // need to redo the entirety of lightning to make it work as intended.
        // so, redo later :godo:
        var strength = sm.InternalEnergy * sm.ZapTransmissionRate * frameTime;
        var strengthNormalized = 1 / (sm.LightningPrototypeIDs.Length - strength) * sm.LightningPrototypeIDs.Length;
        if (strengthNormalized < 0)
            strengthNormalized *= -1;
        _lightning.ShootRandomLightnings(uid, 3, (int) strengthNormalized, sm.LightningPrototypeIDs[(int) strengthNormalized]);
        Comp<RadiationSourceComponent>(uid).Intensity = strength;
    }
    /// <summary>
    ///     React to damage dealt by all doodads.
    /// </summary>
    private void ProcessDamage(EntityUid uid, SupermatterComponent sm)
    {
        CalculateTempLimit(sm);
        sm.DamageArchived = sm.Damage;
        CalculateDamage(sm);

        if (sm.Damage > sm.DelaminationPoint && !sm.AreWeDelaming)
        {
            DelamCountdown(sm);
            return;
        }

        if (sm.Damage > sm.DangerPoint)
        {
            if (_random.Prob(.25f))
                GenerateAnomaly(Comp<TransformComponent>(uid));
            return;
        }
    }
    /// <summary>
    ///     Process waste, release hot plasma and oxygen.
    /// </summary>
    private void ProcessWaste(SupermatterComponent sm)
    {
        CalculateWaste(sm);
        var deviceEnergy = sm.InternalEnergy * Consts.ReactionPowerModifier;

        var mergeMix = sm.AbsorbedGasMix;
        mergeMix.Temperature += deviceEnergy * sm.WasteMultiplier / Consts.ThermalReleaseModifier;
        mergeMix.Temperature = Math.Clamp(mergeMix.Temperature, Atmospherics.TCMB, 2500 * sm.WasteMultiplier);

        mergeMix.AdjustMoles(Gas.Plasma, Math.Max(deviceEnergy * sm.WasteMultiplier / Consts.PlasmaReleaseModifier, 0));
        mergeMix.AdjustMoles(Gas.Oxygen, Math.Max((deviceEnergy + mergeMix.Temperature * sm.WasteMultiplier - Atmospherics.T0C) / Consts.OxygenReleaseModifier, 0));
    }

    /// <summary>
    ///     Swaps out ambience sounds whether the SM is delamming or not.
    /// </summary>
    private void HandleSound(EntityUid uid, SupermatterComponent sm)
    {
        var ambienceComp = Comp<AmbientSoundComponent>(uid);

        if (sm.AreWeDelaming)
        {
            if (sm.CurrentAmbience != sm.DelamAmbienceSound)
                sm.CurrentAmbience = sm.DelamAmbienceSound;
        }
        else if (sm.CurrentAmbience != sm.CalmAmbienceSound)
            sm.CurrentAmbience = sm.CalmAmbienceSound;

        if (ambienceComp.Sound != sm.CurrentAmbience)
            _ambience.SetSound(uid, sm.CurrentAmbience, ambienceComp);
    }

    /// <summary>
    ///     Perform calculation for power lost and gained this cycle.
    /// </summary>
    private Dictionary<string, float> CalculateInternalEnergy(SupermatterComponent sm)
    {
        var additivePower = new Dictionary<string, float>();

        additivePower[Consts.SmPowerExternalTrickle] = sm.ExternalPowerTrickle != 0 ? Math.Max(sm.ExternalPowerTrickle / 10, 40) : 0;
        sm.ExternalPowerTrickle -= Math.Min(additivePower[Consts.SmPowerExternalTrickle], sm.ExternalPowerTrickle);

        additivePower[Consts.SmPowerExternalImmediate] = sm.ExternalPowerImmediate;
        sm.ExternalPowerImmediate = 0f;

        additivePower[Consts.SmPowerHeat] = sm.HeatPowerGeneration * sm.AbsorbedGasMix.Temperature * Consts.GasHeatPowerScaling;

        var momentaryPower = sm.InternalEnergy;
        foreach (var powergainType in additivePower.Keys)
            momentaryPower += additivePower[powergainType];

        if (momentaryPower < sm.PowerlossLinearThreshold) // negative numbers
            additivePower[Consts.SmPowerloss] = (float) Math.Pow(-1 * momentaryPower / 500, 3);
        else
            additivePower[Consts.SmPowerloss] = -1 * momentaryPower / 500 + sm.PowerlossLinearOffset;

        additivePower[Consts.SmPowerlossGas] = -1 * sm.GasPowerlossInhibition * additivePower[Consts.SmPowerloss];

        foreach (var powergainType in additivePower.Keys)
            sm.InternalEnergy += additivePower[powergainType];

        sm.InternalEnergy = Math.Max(sm.InternalEnergy, 0);

        return additivePower;
    }
    /// <summary>
    ///     Perform calculation for the main zap power transmission rate in W/MeV.
    /// </summary>
    private Dictionary<string, float> CalculateZapRate(SupermatterComponent sm)
    {
        var additiveRate = new Dictionary<string, float>();

        additiveRate[Consts.SmZapBase] = Consts.BasePowerTransmissionRate;
        additiveRate[Consts.SmZapGas] = Consts.BasePowerTransmissionRate * sm.PowerTransmissionRate;

        sm.ZapTransmissionRate = 0f;
        foreach (var transmissionType in additiveRate.Keys)
            sm.ZapTransmissionRate += additiveRate[transmissionType];
        sm.ZapTransmissionRate = Math.Max(sm.ZapTransmissionRate, 0);

        return additiveRate;
    }

    /// <summary>
    ///     Calculate at which temperature the SM starts taking damage.
    /// </summary>
    private Dictionary<string, float> CalculateTempLimit(SupermatterComponent sm)
    {
        var additiveTemp = new Dictionary<string, float>();

        var additiveTempBase = Atmospherics.T0C + Consts.HeatPenaltyThreshold;
        additiveTemp[Consts.SmTempLimitBase] = additiveTempBase;
        additiveTemp[Consts.SmTempLimitGas] = sm.GasHeatResistance * additiveTempBase;
        additiveTemp[Consts.SmTempLimitMoles] = Math.Clamp(2 - sm.AbsorbedGasMix.TotalMoles / 100, 0, 1) * additiveTempBase;

        sm.TempLimit = 0f;
        foreach (var resistanceType in additiveTemp.Keys)
            sm.TempLimit += additiveTemp[resistanceType];
        sm.TempLimit = Math.Max(sm.TempLimit, Atmospherics.TCMB);

        return additiveTemp;
    }

    /// <summary>
    ///     Perform calculation for the damage taken or healed.
    /// </summary>
    private Dictionary<string, float> CalculateDamage(SupermatterComponent sm)
    {
        var additiveDamage = new Dictionary<string, float>();

        additiveDamage[Consts.SmDamageExternal] = sm.ExternalDamageImmediate * Math.Clamp((sm.EmergencyPoint - sm.Damage) / sm.EmergencyPoint, 0, 1);
        sm.ExternalDamageImmediate = 0f;

        additiveDamage[Consts.SmDamageHeat] = Math.Clamp((sm.AbsorbedGasMix.Temperature - sm.TempLimit) / 24000, 0, .15f);
        additiveDamage[Consts.SmDamagePower] = Math.Clamp((sm.InternalEnergy - Consts.PowerPenaltyThreshold) / 40000, 0, .1f);
        additiveDamage[Consts.SmDamageMoles] = Math.Clamp((sm.AbsorbedGasMix.TotalMoles - Consts.MolePenaltyThreshold) / 3200, 0, .1f);

        if (sm.AbsorbedGasMix.TotalMoles > 0)
            additiveDamage[Consts.SmDamageHealHeat] = Math.Clamp((sm.AbsorbedGasMix.TotalMoles - sm.TempLimit) / 6000, -.1f, 0);

        var totalDamage = 0f;
        foreach (var damageType in additiveDamage.Keys)
            totalDamage += additiveDamage[damageType];

        sm.Damage += totalDamage;
        sm.Damage = Math.Max(sm.Damage, 0);

        return additiveDamage;
    }

    /// <summary>
    ///     Perform calculation for the waste multiplier.
    ///     This number affects the temperature, plasma, and oxygen of the waste gas.
    ///     Multiplier is applied to energy for plasma and temperature but temperature for oxygen.
    /// </summary>
    private Dictionary<string, float> CalculateWaste(SupermatterComponent sm)
    {
        sm.WasteMultiplier = 0f;
        var additiveWaste = new Dictionary<string, float>();

        additiveWaste[Consts.SmWasteBase] = 1;
        additiveWaste[Consts.SmWasteGas] = sm.GasHeatModifier;

        foreach (var wasteType in additiveWaste.Keys)
            sm.WasteMultiplier += additiveWaste[wasteType];

        sm.WasteMultiplier = Math.Clamp(sm.WasteMultiplier, .5f, float.PositiveInfinity);
        return additiveWaste;
    }

    /// <summary>
    ///     Handle supermatter delamination and the end of the station.
    /// </summary>
    private void Delaminate(SupermatterComponent sm)
    {

    }
    /// <summary>
    ///     Choose a prefered delamination type. The supermatter is picky.
    /// </summary>
    private DelamType ChooseDelam(SupermatterComponent sm)
    {
        if (sm.AbsorbedGasMix.TotalMoles >= Consts.MolePenaltyThreshold)
            return DelamType.Singularity;

        if (sm.InternalEnergy > Consts.PowerPenaltyThreshold)
            return DelamType.Tesla;

        // todo: add resonance cascade when hypernob and antinob gases get added
        // or a destabilizing crystal. bet it will take years :godo:

        return DelamType.Explosion;
    }
    /// <summary>
    ///     Handle the delamination countdown, alerts, etc.
    /// </summary>
    private void DelamCountdown(SupermatterComponent sm)
    {

    }

    private void GenerateAnomaly(TransformComponent xform, float amount = 1)
    {
        if (_station.GetStationInMap(xform.MapID) is not { } station ||
            !TryComp<StationDataComponent>(station, out var data) ||
            _station.GetLargestGrid(data) is not { } grid)
        {
            if (xform.GridUid == null)
                return;
            grid = xform.GridUid.Value;
        }

        for (var i = 0; i < amount; i++)
            _anomaly.SpawnOnRandomGridLocation(grid, "RandomAnomalySpawner");
    }

    /// <summary>
    ///     Make console alerts, set station codes, etc. etc.
    /// </summary>
    public void SupermatterAlert(string message, bool isDelamming = true, DelamType? delamType = null)
    {

    }

    /// <summary>
    ///     Vaporizes anything that touched the SM. @o7.
    /// </summary>
    private void OnCollide(EntityUid uid, SupermatterComponent sm, StartCollideEvent args)
    {

    }

    /// <summary>
    ///     Event handlet to get the supermatter sliver using any knife.
    /// </summary>
    private void OnClick(EntityUid uid, SupermatterComponent sm, InteractUsingEvent args)
    {
        if (!TryComp<TagComponent>(args.Used, out var tags))
            return;
        foreach (var tag in tags.Tags)
        {
            if (tag == "Knife")
            {
                _popup.PopupClient(Loc.GetString("supermatter-tamper-begin"), args.User);

                new DoAfterArgs(EntityManager, uid, 30, new SupermatterDoAfterEvent(), args.Used)
                {
                    BreakOnDamage = true,
                    BreakOnHandChange = true,
                    BreakOnMove = true,
                    BreakOnWeightlessMove = true,
                    NeedHand = true,
                    RequireCanInteract = true,
                };
            }
        }
    }
    /// <summary>
    ///     Handles supermatter sliver objective completion.
    /// </summary>
    private void OnGetSliver(EntityUid uid, SupermatterComponent sm, SupermatterDoAfterEvent args)
    {
        sm.Damage += 10; // your criminal actions will not go unnoticed
        SupermatterAlert(Loc.GetString("supermatter-announcement-tamper", ("integrity", sm.Integrity)));

        Spawn(sm.SliverPrototype, _transform.GetMapCoordinates(args.User));
        _popup.PopupClient(Loc.GetString("supermatter-tamper-end"), args.User);
    }

    /// <summary>
    ///     Handles external damage, e.g. someone L6-ing the SM.
    /// </summary>
    private void OnGetHit(EntityUid uid, SupermatterComponent sm, DamageChangedEvent args)
    {

    }
}

[Serializable, NetSerializable]
public sealed partial class SupermatterDoAfterEvent : SimpleDoAfterEvent {}
