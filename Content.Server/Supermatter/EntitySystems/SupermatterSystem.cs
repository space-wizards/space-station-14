using System;
using Robust.Shared.Audio;
using Content.Server.Radiation;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Supermatter.Components;
using Content.Shared.Body.Components;
using Content.Server.Ghost.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Content.Shared.SubFloor;
using Content.Shared.Damage;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Server.Projectiles.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Player;
using Content.Shared.Atmos;

namespace Content.Server.Supermatter.EntitySystems
{
    [UsedImplicitly]
    public class SupermatterSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly RadiationSystem _radiationSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly TransformComponent _transformComponent = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly ExplosionSystem _explosions = default!;

        public override void Initialize()
        {
            base.Initialize();
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var supermatter in EntityManager.EntityQuery<SupermatterComponent>())
            {
                HandleBehavour(supermatter.OwnerUid, _transformComponent.WorldPosition, frameTime, supermatter);
            }
        }


        private float _atmosUpdateAccumulator;
        //update atmos every half second
        private const float _atmosUpdateTimer = 0.5f;

        /// <summary>
        /// Handle outputting radiation based off enery, damage, and gas mix
        /// </summary>
        public void HandleRads(EntityUid Uid, float frameTime, SupermatterComponent? component = null)
        {
            if(!Resolve(Uid, ref component))
            return;

            _atmosUpdateAccumulator += frameTime;
            if(_atmosphereSystem.GetTileMixture(_transformComponent.Coordinates) is { } mixture && _atmosUpdateAccumulator > _atmosUpdateTimer)
            {
                _atmosUpdateAccumulator -= _atmosUpdateTimer;
                component.Mix = mixture;
                var pressure = mixture.Pressure;
                var moles = mixture.TotalMoles;
                var temp = mixture.Temperature;

                if(moles > 0f)
                {
                    var Oxy = mixture.Moles[0];
                    var Nit = mixture.Moles[1];
                    var Co2 = mixture.Moles[2];
                    var Pla = mixture.Moles[3];
                    var Tri = mixture.Moles[4];
                    var H2o = mixture.Moles[5];

                    for(int gasId = 0; gasId < component.GasComp.Length; gasId++)
                    {
                        component.GasComp[gasId] = Math.Clamp(mixture.Moles[gasId] / moles, 0, 1);
                    };

                    var h2oBonus = 1 - (component.GasComp[5] * 0.25f);


                    component.GasmixPowerRatio = 0f;
                    component.DynamicHeatModifier = 0f;
                    component.PowerTransmissionBonus = 0f;
                    for(int gasId = 0; gasId < component.GasComp.Length; gasId++)
                    {
                        //No less then zero, and no greater then one, we use this to do explosions and heat to power transfer
                        //Be very careful with modifing this var by large amounts, and for the love of god do not push it past 1
                        component.GasmixPowerRatio += component.GasComp[gasId] * component.gasFacts[gasId, 2];

                        //Minimum value of -10, maximum value of 23. Effects plasma and o2 output and the output heat
                        component.DynamicHeatModifier += component.GasComp[gasId] + component.gasFacts[gasId, 1];

                        //Value between -5 and 30, used to determine radiation output as it concerns things like collectors.
                        component.PowerTransmissionBonus += component.GasComp[gasId] + component.gasFacts[gasId, 0];
                    };
                    component.GasmixPowerRatio = Math.Clamp(component.GasmixPowerRatio, 0, 1);
                    component.DynamicHeatModifier = Math.Max(component.DynamicHeatModifier, 0.5f);
                    component.PowerTransmissionBonus *= h2oBonus;

                    //Value between 1 and 10. Effects the damage heat does to the crystal
                    component.DynamicHeatResistance = 0f;

                    //TODO: port this once more gasses (cough n20) are in
                    /*
                    for(var/gas_id in gas_resist)
                        dynamic_heat_resistance += gas_comp[gas_id] * gas_resist[gas_id])
                    dynamic_heat_resistance = max(dynamic_heat_resistance, 1)
                    */

                    //TODO: port this once miasma is in
                    /*
                    if(gas_comp[/datum/gas/miasma])
                        var/miasma_pp = env.return_pressure() * gas_comp[/datum/gas/miasma]
                        var/consumed_miasma = clamp(((miasma_pp - MIASMA_CONSUMPTION_PP) / (miasma_pp + MIASMA_PRESSURE_SCALING)) * (1 + (gasmix_power_ratio * MIASMA_GASMIX_SCALING)), MIASMA_CONSUMPTION_RATIO_MIN, MIASMA_CONSUMPTION_RATIO_MAX)
                        consumed_miasma *= gas_comp[/datum/gas/miasma] * combined_gas
                        if(consumed_miasma)
                            removed.gases[/datum/gas/miasma][MOLES] -= consumed_miasma
                            matter_power += consumed_miasma * MIASMA_POWER_GAIN
                    */

                    //more moles of gases are harder to heat than fewer, so let's scale heat damage around them
                    var MoleHeatPenalty = Math.Max(moles / component.MoleHeatPenalty, 0.25);

                    //Ramps up or down in increments of 0.02 up to the proportion of co2
                    //Given infinite time, powerloss_dynamic_scaling = co2comp
                    //Some value between 0 and 1

                    if(moles > SupermatterComponent.PowerlossInhibitionMoleThreshold && component.GasComp[2] > SupermatterComponent.PowerlossInhibitionGasThreshold)
                        component.PowerlossDynamicScaling = Math.Clamp(component.PowerlossDynamicScaling + Math.Clamp(component.GasComp[2] - component.PowerlossDynamicScaling, -0.02f, 0.02f), 0f, 1f);
                    else
                        component.PowerlossDynamicScaling = Math.Clamp(component.PowerlossDynamicScaling - 0.05f, 0f, 1f);

                    component.PowerlossInhibitor = Math.Clamp(1-(component.PowerlossDynamicScaling * Math.Clamp(moles/SupermatterComponent.PowerlossInhibitionMoleBoostThreshold, 1f, 1.5f)), 0f, 1f);

                    float TempFactor;
                    if(component.GasmixPowerRatio > 0.8)
                    {
                        //with a perfect gas mix, make the power more based on heat
                        TempFactor = 50f;
                        //glow harder
                    }
                    else
                    {
                        //in normal mode, power is less effected by heat
                        TempFactor = 30f;
                        //glow normally
                    }

                    component.Power = Math.Max((temp * TempFactor / Atmospherics.T0C) * component.GasmixPowerRatio + component.Power, 0);

                    _radiationSystem.Radiate(component.Power * Math.Max(0f, (1f + (component.PowerTransmissionBonus/10f))), 10f, component, frameTime);

                    //TODO: PsyCoeff should change from 0-1 based on psycologist distance
                    float energy = component.Power * SupermatterComponent.ReactionPowerModefier * (1f - (component.PsyCoeff * 0.2f));

                    //To figure out how much temperature to add each tick, consider that at one atmosphere's worth
                    //of pure oxygen, with all four lasers firing at standard energy and no N2 present, at room temperature
                    //that the device energy is around 2140. At that stage, we don't want too much heat to be put out
                    //Since the core is effectively "cold"

                    //Also keep in mind we are only adding this temperature to (efficiency)% of the one tile the rock
                    //is on. An increase of 4*C @ 25% efficiency here results in an increase of 1*C / (#tilesincore) overall.
                    //Power * 0.55 * (some value between 1.5 and 23) / 5

                    temp += ((energy * component.DynamicHeatModifier) / SupermatterComponent.ThermalReleaseModifier);

                    //We can only emit so much heat, that being 57500
                    temp = Math.Max(0, Math.Min(temp, 2500 * component.DynamicHeatModifier));

                    //Calculate how much gas to release
                    //Varies based on power and gas content
                    Pla += Math.Max(((energy * component.DynamicHeatModifier) / SupermatterComponent.PlasmaReleaseModifier), 0f);
                    //Varies based on power, gas content, and heat
                    Oxy += Math.Max((((energy + temp * component.DynamicHeatModifier) - Atmospherics.T0C) / SupermatterComponent.OxygenReleaseModifier), 0f);

                    component.Power = Math.Clamp(component.Power - Math.Min(((float) Math.Pow(component.Power / 500f, 3f) * component.PowerlossInhibitor), component.Power * 0.83f * component.PowerlossInhibitor) * (1f - (0.2f * component.PsyCoeff)), 0f, 10000f);
                }
            }
        }

        private float _damageUpdateAccumulator;
        //update environment damage every second
        private const float _damageUpdateTimer = 1f;
        public void HandleDamage(EntityUid Uid, float frameTime, SupermatterComponent? component = null, DamageableComponent? damageable = null)
        {
            if(!Resolve(Uid, ref component, ref damageable))
                return;

            _damageUpdateAccumulator += frameTime;
            float damage = 0;
            component.DamageArchived = _entityManager.GetComponent<DamageableComponent>(Uid).TotalDamage.Float();
            var integrity = GetIntegrity(Uid, component);

            if(component.DamageArchived >= SupermatterComponent.ExplosionPoint)
            {
                Delamination(Uid, frameTime, component);
                return;
            }

            if(component.DamageArchived <= SupermatterComponent.ExplosionPoint)
            {
                component.YellAccumulator += frameTime;
                if(component.YellAccumulator >= SupermatterComponent.YellTimer)
                {
                    if(component.DamageArchived >= SupermatterComponent.EmergencyPoint && component.DamageArchived <= SupermatterComponent.ExplosionPoint)
                    {
                        _chatManager.DispatchStationAnnouncement($"WARNING! Crystal hyperstructure integrity reaching critical levels! Integrity:{100 - integrity}%", "Supermatter");
                        component.YellAccumulator = 0;
                    }
                    if(component.DamageArchived >= SupermatterComponent.WarningPoint && component.DamageArchived <= SupermatterComponent.EmergencyPoint)
                    {
                        _chatManager.EntitySay(component.Owner, $"Danger! Crystal hyperstructure integrity faltering! Integrity:{100 - integrity}%");
                        component.YellAccumulator = 0;
                    }

                }
            }

            if(_damageUpdateAccumulator > _damageUpdateTimer)
            {
                _damageUpdateAccumulator -= _damageUpdateTimer;
                //if in space
                if(!_transformComponent.GridID.IsValid())
                {
                    damage = Math.Max((component.Power / 1000) * SupermatterComponent.DamageIncreaseMultiplier, 0.1f);
                }

                //*
                //if in an atmosphere
                if(_atmosphereSystem.GetTileMixture(_transformComponent.Coordinates) is { } mixture)
                {
                    //((((some value between 0.5 and 1 * temp - ((273.15 + 40) * some values between 1 and 10)) * some number between 0.25 and knock your socks off / 150) * 0.25
                    //Heat and mols account for each other, a lot of hot mols are more damaging then a few
                    //Mols start to have a positive effect on damage after 350
                    var MoleClamp = Math.Clamp(mixture.TotalMoles / 200f, 0.5f, 1f);
                    var HeatDamage = ((Atmospherics.T0C + SupermatterComponent.HeatPenaltyThreshold)*component.DynamicHeatResistance);
                    damage = Math.Max(damage + (Math.Max(MoleClamp * mixture.Temperature - HeatDamage, 0) * component.MoleHeatPenalty / 150) * SupermatterComponent.DamageIncreaseMultiplier, 0f);
                    //Power only starts affecting damage when it is above 5000
                    damage = Math.Max(damage + (Math.Max(component.Power - SupermatterComponent.PowerPenaltyThreshold, 0f)/500f) * SupermatterComponent.DamageIncreaseMultiplier, 0f);
                    //Molar count only starts affecting damage when it is above 1800
                    damage = Math.Max(damage + (Math.Max(mixture.TotalMoles - SupermatterComponent.MolePenaltyThreshold, 0f)/80f) * SupermatterComponent.DamageIncreaseMultiplier, 0f);

                    //There might be a way to integrate healing and hurting via heat
			        //healing damage
                    if(mixture.TotalMoles < SupermatterComponent.MolePenaltyThreshold)
                    {
                        //Only has a net positive effect when the temp is below 313.15, heals up to 2 damage. Psycologists increase this temp min by up to 45
                        var heatcap = ((Atmospherics.T0C + SupermatterComponent.HeatPenaltyThreshold) + (45 * component.PsyCoeff));
                        damage = Math.Max(-(2 * (1 - (mixture.Temperature / heatcap))),-2);
                    }
                    //if there are space tiles next to SM
                    foreach(var adjacent in _atmosphereSystem.GetAdjacentTileMixtures(_transformComponent.Coordinates))
                    {
                        if(adjacent.TotalMoles == 0)
                        {
                            if(integrity < 10)
                                damage = Math.Clamp((component.Power * 0.0005f) * SupermatterComponent.DamageIncreaseMultiplier, 0f, SupermatterComponent.MaxSpaceExposureDamage);
                            else if(integrity < 25)
                                damage = Math.Clamp((component.Power * 0.0009f) * SupermatterComponent.DamageIncreaseMultiplier, 0f, SupermatterComponent.MaxSpaceExposureDamage);
                            else if(integrity < 45)
                                damage = Math.Clamp((component.Power * 0.005f) * SupermatterComponent.DamageIncreaseMultiplier, 0f, SupermatterComponent.MaxSpaceExposureDamage);
                            else if(integrity < 75)
                                damage = Math.Clamp((component.Power * 0.002f) * SupermatterComponent.DamageIncreaseMultiplier, 0f, SupermatterComponent.MaxSpaceExposureDamage);
                            break;
                        }
                    }
                }
                //only take 1.8 per cycle //gets rouneded to 2 after the first cycle??? why??? the float to fixed cast shouldnt be rounding???
                damage = Math.Min(component.DamageArchived + (SupermatterComponent.DamageHardcap * SupermatterComponent.ExplosionPoint), damage);
                //damage to add to total
                var damageDelta = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), (Shared.FixedPoint.FixedPoint2)damage);
                _damageable.TryChangeDamage(component.OwnerUid, damageDelta, true);
            }
        }

        public float GetIntegrity(EntityUid Uid, SupermatterComponent? component = null)
        {
            if(!Resolve(Uid, ref component))
            {
                return 0f;
            }
            var damage = component.Owner.GetComponent<DamageableComponent>().TotalDamage;
            var integrity = 100 * ((double) damage / SupermatterComponent.ExplosionPoint);
            integrity = Math.Round(integrity);
            integrity = integrity < 0 ? 0 : integrity;
            return(float) integrity;
        }

        private float _delamTimerAccumulator;
        private const int _delamTimerTimer = 30;
        private float _speakAccumulator = 5f;
        private bool AlarmPlaying = false;
        public void Delamination(EntityUid Uid, float frameTime, SupermatterComponent? component = null)
        {
            if(!Resolve(Uid, ref component))
                return;
            //TODO: make tesla spawn at SupermatterComponent.PowerPenaltyThreshold
            if(!component.FinalCountdown)
            {
                _chatManager.DispatchStationAnnouncement("The Supermatter has Reached Critical Integrity Falure. Emergency Causality Destabilization Field has been Activated.", "Supermatter");
            }
            component.FinalCountdown = true;

            _delamTimerAccumulator += frameTime;
            _speakAccumulator += frameTime;
            var RoundSeconds = _delamTimerTimer - (int)Math.Floor(_delamTimerAccumulator);

            if(component.DamageArchived < SupermatterComponent.ExplosionPoint)
            {
                _chatManager.DispatchStationAnnouncement($"{SupermatterComponent.SafeAlert} Failsafe has been Disengaged.");
                component.FinalCountdown = false;
                return;
            }
            else if(RoundSeconds >= 5 && _speakAccumulator >= 5)
            {
                _speakAccumulator -= 5;
                _chatManager.DispatchStationAnnouncement($"{RoundSeconds} Seconds Remain Before Delamination.", "Supermatter");
            }
            else if(RoundSeconds <  5 && _speakAccumulator >= 1)
            {
                _speakAccumulator -= 1;
                _chatManager.DispatchStationAnnouncement($"{RoundSeconds} Seconds Remain Before Delamination.", "Supermatter");
            }
            if(_delamTimerAccumulator >= _delamTimerTimer)
            {
                if(_atmosphereSystem.GetTileMixture(_transformComponent.Coordinates) is { } mixture)
                {
                    if(mixture.TotalMoles >= SupermatterComponent.MolePenaltyThreshold)
                    {
                        _entityManager.SpawnEntity("Singularity", _transformComponent.Coordinates);
                        return;
                    }
                }
                //TODO: tune this after explosion refactor
                _explosions.SpawnExplosion(_transformComponent.Coordinates, 75, 75, 75, 100);
                _entityManager.QueueDeleteEntity(Uid);
            }

            if(component.FinalCountdown && !AlarmPlaying && RoundSeconds <= 13)
            {
                AlarmPlaying = true;
                SoundSystem.Play(Filter.Pvs(Uid, 2), component.DelamAlarm.GetSound(), Uid);
            }
        }

        public bool CanDestroy(EntityUid Uid)
        {
            return _entityManager.HasComponent<SharedBodyComponent>(Uid) || _entityManager.HasComponent<SharedItemComponent>(Uid) ||
            _entityManager.GetComponent<TagComponent>(Uid).HasTag("EmitterBolt");
        }

        public bool CannotDestroy(EntityUid Uid)
        {
            return _entityManager.GetComponent<TagComponent>(Uid).HasTag("SMimmune") ||
            _entityManager.GetEntity(Uid).IsInContainer() || _entityManager.HasComponent<GhostComponent>(Uid) ||
            _entityManager.HasComponent<SubFloorHideComponent>(Uid);
        }
        public void HandleDestroy(EntityUid Uid, SupermatterComponent? component = null)
        {
            if(!Resolve(Uid, ref component))
                return;
            if(!CanDestroy(Uid) || CannotDestroy(Uid)) return;

            _entityManager.SpawnEntity("Ash", _transformComponent.MapPosition);
            _entityManager.QueueDeleteEntity(Uid);

            if(_entityManager.TryGetComponent<SupermatterFoodComponent>(Uid, out var SupermatterFood))
            {
                component.Power += SupermatterFood.Energy;
            }
            else if(_entityManager.TryGetComponent<ProjectileComponent>(Uid, out var Projectile))
            {
                component.Power += (float) Projectile.Damage.Total;
            }
            else
            {
                component.Power++;
            }
        }

        /// <summary>
        /// Handle getting entities in range and calling behavour
        /// </summary>
        public void HandleBehavour(EntityUid Uid, Vector2 worldPos, float frameTime, SupermatterComponent? component = null)
        {
            if(!Resolve(Uid, ref component))
                return;
            HandleRads(Uid, frameTime, component);
            HandleDamage(Uid, frameTime, component);
            foreach(var entity in _lookup.GetEntitiesInRange(_transformComponent.MapID, worldPos, 0.5f))
            {
                HandleDestroy(Uid, component);
            }
        }
    }
}
