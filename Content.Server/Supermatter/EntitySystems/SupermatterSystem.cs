using System;
using System.Data;
using System.Linq;
using Content.Shared.Radiation;
using Content.Server.Radiation;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Supermatter;
using Content.Server.Supermatter.Components;
using Content.Shared.Body.Components;
using Content.Server.Ghost.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Content.Server.Body;
using Content.Shared.SubFloor;
using Content.Server.Doors.Components;
using Content.Shared.Doors;
using Content.Shared.Damage;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Content.Shared.Tag;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Projectiles;
using Content.Server.Projectiles.Components;
using Content.Server.Explosion;
using Content.Server.Radio.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.Chat.Managers;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using Content.Shared.Acts;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.ViewVariables;
using Content.Shared.Atmos;
using Content.Server.Alert;
using Content.Server.Atmos.Components;
using Content.Shared.Alert;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SupermatterSystem{
    [UsedImplicitly]

    public class SupermatterSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly RadiationPulseSystem _radiationPulseSystem = default!;
        [Dependency] private readonly RadiationSystem _radiationSystem = default!;

        [Dependency] private IRobustRandom _robustRandom = default!;
        [Dependency] private IEntityManager _entityManager = default!;
        private RadioSystem _radioSystem = default!;

        private float _updateAccumulator;
        private const float _updateTimer = 0.5f;

        //private const float RadiationCooldown = 0.5f;
        //private float _accumulator;
        public override void Initialize()
        {
            base.Initialize();
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var supermatter in EntityManager.EntityQuery<SupermatterComponent>())
            {
                Update(supermatter, frameTime);
            }
        }

        /// <summary>
        /// Handle outputting radiation based off enery, damage, and gas mix
        /// </summary>
        public void HandleRads(SupermatterComponent component, float frameTime)
        {
            _updateAccumulator += frameTime;
            if (_atmosphereSystem.GetTileMixture(component.Owner.Transform.Coordinates) is { } mixture && _updateAccumulator > _updateTimer)
            {
                _updateAccumulator -= _updateTimer;
                var pressure = mixture.Pressure;
                var moles = mixture.TotalMoles;
                var temp = mixture.Temperature;

                var Oxy = mixture.Moles[0];
                var Nit = mixture.Moles[1];
                var Co2 = mixture.Moles[2];
                var Pla = mixture.Moles[3];
                var Tri = mixture.Moles[4];
                var H2o = mixture.Moles[5];

                for (int gasId = 0; gasId < component.GasComp.Length; gasId++)
                {
                    component.GasComp[gasId] = Math.Clamp(mixture.Moles[gasId] / moles, 0, 1);
                };

                var h2oBonus = 1 - (component.GasComp[5] * 0.25f);


                component.GasmixPowerRatio = 0f;
                component.DynamicHeatModifier = 0f;
                component.PowerTransmissionBonus = 0f;
                for (int gasId = 0; gasId < component.GasComp.Length; gasId++)
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
                //TODO: port this once more gasses are in
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

                if (moles > SupermatterComponent.PowerlossInhibitionMoleThreshold && component.GasComp[2] > SupermatterComponent.PowerlossInhibitionGasThreshold)
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

                component.Power = Math.Max((temp * TempFactor / 273.15f) * component.GasmixPowerRatio + component.Power, 0);

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
                Oxy += Math.Max((((energy + temp * component.DynamicHeatModifier) - 273.15f) / SupermatterComponent.OxygenReleaseModifier), 0f);

                component.Power = Math.Clamp(component.Power - Math.Min(((float) Math.Pow(component.Power / 500f, 3f) * component.PowerlossInhibitor), component.Power * 0.83f * component.PowerlossInhibitor) * (1f - (0.2f * component.PsyCoeff)), 0f, 10000f);
            }
            else if(_atmosphereSystem.GetTileMixture(component.Owner.Transform.Coordinates) is null)
            {
                var temp = 0f;
                component.GasmixPowerRatio = 0;

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

                component.Power = Math.Max((temp * TempFactor / 273.15f) * component.GasmixPowerRatio + component.Power, 0);
            }
        }

        public bool CanDestroy(IEntity entity)
        {
            return entity.HasComponent<SharedBodyComponent>() || entity.HasComponent<SharedItemComponent>() ||
            entity.HasTag("EmitterBolt");
        }

        public bool CantDestroy(IEntity entity)
        {
            return entity.MetaData.EntityName == "ash" || entity.IsInContainer() ||
            entity.HasComponent<GhostComponent>() || entity.HasComponent<SubFloorHideComponent>();
        }

        public void HandleDamage(SupermatterComponent component, float frameTime)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            var entity = component.Owner;
            float damage = (float)component.Owner.GetComponent<DamageableComponent>().TotalDamage;
            var damageArchived = component.DamageArchived;
            var integrity = GetIntegrity(component);

            //*
            //if in an atmosphere
            if(_atmosphereSystem.GetTileMixture(component.Owner.Transform.Coordinates) is { } mixture)
            {
                //((((some value between 0.5 and 1 * temp - ((273.15 + 40) * some values between 1 and 10)) * some number between 0.25 and knock your socks off / 150) * 0.25
                //Heat and mols account for each other, a lot of hot mols are more damaging then a few
                //Mols start to have a positive effect on damage after 350
                var MoleClamp = Math.Clamp(mixture.TotalMoles / 200f, 0.5f, 1f);
                var HeatDamage = ((275.15f + SupermatterComponent.HeatPenaltyThreshold)*component.DynamicHeatResistance);
                damage = Math.Max(damage + (Math.Max(MoleClamp * mixture.Temperature - HeatDamage, 0) * component.MoleHeatPenalty / 150) * SupermatterComponent.DamageIncreaseMultiplier, 0);
                //Power only starts affecting damage when it is above 5000
                damage = Math.Max(damage + (Math.Max(component.Power - SupermatterComponent.PowerPenaltyThreshold, 0f)/500f) * SupermatterComponent.DamageIncreaseMultiplier, 0f);
                //Molar count only starts affecting damage when it is above 1800
                damage = Math.Max(damage + (Math.Max(mixture.TotalMoles - SupermatterComponent.MolePenaltyThreshold, 0)/80f) * SupermatterComponent.DamageIncreaseMultiplier, 0);

                damage = Math.Min(component.DamageArchived + (SupermatterComponent.DamageHardcap * 900f), damage);
            } //*/

            /*
            //if there are space tiles next to SM
            if()
            {
                if(integrity < 10)
                    damage = Math.Clamp((component.Power * 0.0005f) * SupermatterComponent.DamageIncreaseMultiplier, 0, SupermatterComponent.MaxSpaceExposureDamage);
                else if(integrity < 25)
                    damage = Math.Clamp((component.Power * 0.0009f) * SupermatterComponent.DamageIncreaseMultiplier, 0, SupermatterComponent.MaxSpaceExposureDamage);
                else if(integrity < 45)
                    damage = Math.Clamp((component.Power * 0.005f) * SupermatterComponent.DamageIncreaseMultiplier, 0, SupermatterComponent.MaxSpaceExposureDamage);
                else if(integrity < 75)
                    damage = Math.Clamp((component.Power * 0.002f) * SupermatterComponent.DamageIncreaseMultiplier, 0, SupermatterComponent.MaxSpaceExposureDamage);
            } //*/

            //if in space
            if((int)component.Owner.Transform.GridID == 0)
            {
                damage = Math.Max((component.Power / 1000) * SupermatterComponent.DamageIncreaseMultiplier, 0.1f);
            }

            damage = Math.Min(component.DamageArchived + (SupermatterComponent.DamageHardcap * 900f), damage);

            //damage = ((decimal)damage);

            var damageDelta = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Shock"), (Shared.FixedPoint.FixedPoint2)damage);
            _damageable.TryChangeDamage(component.OwnerUid, damageDelta, true);

            //entity.GetComponent<DamageableComponent>().TotalDamage = damage;

            //_mapManager.GetGrid(component.Owner.Transform.GridID).TryGetTileRef(component.Owner.Transform.MapPosition.Position, out var TileRef)
            /*
            var splodeTime = 180f;
            var hurtTime = 120f;
            if (damage >= 900)
            {
                Delamination(component, entity, frameTime);
                entity.SpawnExplosion(50, 50, 50, 75); //TODO: Tune This once splosions work:tm:
                entity.QueueDelete();
                return;
            }

            component.splodeTimer += frameTime;
            while (damage >= 700 & component.splodeTimer > splodeTime)
            {
                component.splodeTimer -= splodeTime;
                component.emergency_point = true;
                chat.DispatchStationAnnouncement("CRYSTAL DELAMINATION IMMINENT.", "Supermatter");
                //TODO: yell over comms about blowing the fuck up
            }

            component.hurtTimer += frameTime;
            while (damage >= 50 & damage <= 700 & component.hurtTimer > hurtTime)
            {
                component.hurtTimer -= hurtTime;
                component.warning_point = true;
                chat.DispatchStationAnnouncement("Danger! Crystal hyperstructure integrity faltering!", "Supermatter");
                //TODO: yell over comms about being bullied
            }
            */



        }

        public float GetIntegrity(SupermatterComponent component)
        {
            var damage = component.Owner.GetComponent<DamageableComponent>().TotalDamage;
            var integrity = (double) damage / 900;
            integrity = Math.Round(integrity);
            integrity = integrity < 0 ? 0 : integrity;
            return (float) integrity;
        }

        public void Delamination(SupermatterComponent component, IEntity entity, float frameTime)
        {

        }

        public void HandleDestroy(SupermatterComponent component, IEntity entity)
        {
            if (!CanDestroy(entity) || CantDestroy(entity)) return;

            EntityManager.SpawnEntity("Ash", entity.Transform.MapPosition);
            entity.QueueDelete();

            if (entity.TryGetComponent<SupermatterFoodComponent>(out var SupermatterFood))
            {
                component.Power += SupermatterFood.Energy;
            }
            else if (entity.TryGetComponent<ProjectileComponent>(out var Projectile))
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
        public void HandleBehavour(SupermatterComponent component, Vector2 worldPos, float frameTime){
            HandleRads(component, frameTime);
            HandleDamage(component, frameTime);
            foreach (var entity in _lookup.GetEntitiesInRange(component.Owner.Transform.MapID, worldPos, 0.5f))
            {
                HandleDestroy(component, entity);
            }
        }

        public void Update(SupermatterComponent component, float frameTime)
        {
            var worldPos = component.Owner.Transform.WorldPosition;
            HandleBehavour(component, worldPos, frameTime);
        }
    }
}
