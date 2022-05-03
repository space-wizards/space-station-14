using System;
using Robust.Shared.Audio;
using Content.Server.Radiation;
using JetBrains.Annotations;
using Content.Server.Supermatter.Components;
using Robust.Shared.Containers;
using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Server.Projectiles.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Explosion.Components;
using Content.Server.Chat.Managers;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Player;
using Content.Shared.Atmos;
using Robust.Shared.Physics.Dynamics;
using Content.Server.Chat;
using Content.Server.Ghost.Components;

namespace Content.Server.Supermatter.EntitySystems
{
    [UsedImplicitly]
    public class SupermatterSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SupermatterComponent, StartCollideEvent>(OnCollideEvent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (Supermatter, damageable, Xform, Xplode) in EntityManager.EntityQuery<SupermatterComponent, DamageableComponent, TransformComponent, ExplosiveComponent>())
            {
                HandleRads(Supermatter.Owner, frameTime, Supermatter, Xform);
                HandleDamage(Supermatter.Owner, frameTime, Supermatter, damageable, Xform, Xplode);
            }
        }

        /// <summary>
        /// Handle outputting radiation based off enery, damage, and gas mix
        /// </summary>
        public void HandleRads(EntityUid Uid, float frameTime, SupermatterComponent? SMcomponent = null, TransformComponent? Xform = null, RadiationPulseComponent? rad = null)
        {
            if(!Resolve(Uid, ref SMcomponent, ref Xform, ref rad))
            {
                return;
            }

            SMcomponent.AtmosUpdateAccumulator += frameTime;
            if (_atmosphere.GetTileMixture(Xform.Coordinates) is { } mixture && SMcomponent.AtmosUpdateAccumulator > SupermatterComponent.AtmosUpdateTimer)
            {
                SMcomponent.AtmosUpdateAccumulator -= SupermatterComponent.AtmosUpdateTimer;
                SMcomponent.Mix = mixture;
                var pressure = mixture.Pressure;
                var moles = mixture.TotalMoles;
                var temp = mixture.Temperature;

                if (moles > 0f)
                {
                    //grab the moles for each gas
                    var Oxy = mixture.Moles[0];
                    var Nit = mixture.Moles[1];
                    var Co2 = mixture.Moles[2];
                    var Pla = mixture.Moles[3];
                    var Tri = mixture.Moles[4];
                    var H2o = mixture.Moles[5];

                    //get the moles of each gas as a percent from 0 to 1
                    for(int gasId = 0; gasId < SMcomponent.GasComp.Length; gasId++)
                    {
                        SMcomponent.GasComp[gasId] = Math.Clamp(mixture.Moles[gasId] / moles, 0, 1);
                    };

                    SMcomponent.GasmixPowerRatio = 0f;
                    SMcomponent.DynamicHeatModifier = 0f;
                    SMcomponent.PowerTransmissionBonus = 0f;
                    for(int gasId = 0; gasId < SMcomponent.GasComp.Length; gasId++)
                    {
                        //TODO: activate this once NO2/Hydrogen/proto nitrate is in
                        //Value between 1 and 10. Effects the damage heat does to the crystal
                        //SMcomponent.DynamicHeatResistance += SMcomponent.GasComp[gasId] + SMcomponent.gasFacts[gasId, 3];

                        //No less then zero, and no greater then one, we use this to do explosions and heat to power transfer
                        //Be very careful with modifing this var by large amounts, and for the love of god do not push it past 1
                        SMcomponent.GasmixPowerRatio += SMcomponent.GasComp[gasId] * SMcomponent.gasFacts[gasId, 2];

                        //Minimum value of -10, maximum value of 23. Effects plasma and o2 output and the output heat
                        SMcomponent.DynamicHeatModifier += SMcomponent.GasComp[gasId] + SMcomponent.gasFacts[gasId, 1];

                        //Value between -5 and 30, used to determine radiation output as it concerns things like collectors.
                        SMcomponent.PowerTransmissionBonus += SMcomponent.GasComp[gasId] + SMcomponent.gasFacts[gasId, 0];
                    };
                    //SMcomponent.DynamicHeatResistance = Math.Max(SMcomponent.DynamicHeatResistance, 1);
                    SMcomponent.DynamicHeatResistance = 1f;
                    SMcomponent.GasmixPowerRatio = Math.Clamp(SMcomponent.GasmixPowerRatio, 0, 1);
                    SMcomponent.DynamicHeatModifier = Math.Max(SMcomponent.DynamicHeatModifier, 0.5f);

                    var h2oBonus = 1 - (SMcomponent.GasComp[5] * 0.25f);
                    SMcomponent.PowerTransmissionBonus *= h2oBonus;

                    //TODO: port miasma(?)

                    //more moles of gases are harder to heat than fewer, so let's scale heat damage around them
                    var MoleHeatPenalty = Math.Max(moles / SMcomponent.MoleHeatPenalty, 0.25);

                    //Ramps up or down in increments of 0.02 up to the proportion of co2
                    //Given infinite time, powerloss_dynamic_scaling = co2comp
                    //Some value between 0 and 1

                    if (moles > SupermatterComponent.PowerlossInhibitionMoleThreshold && SMcomponent.GasComp[2] > SupermatterComponent.PowerlossInhibitionGasThreshold)
                        SMcomponent.PowerlossDynamicScaling = Math.Clamp(SMcomponent.PowerlossDynamicScaling + Math.Clamp(SMcomponent.GasComp[2] - SMcomponent.PowerlossDynamicScaling, -0.02f, 0.02f), 0f, 1f);
                    else
                        SMcomponent.PowerlossDynamicScaling = Math.Clamp(SMcomponent.PowerlossDynamicScaling - 0.05f, 0f, 1f);

                    SMcomponent.PowerlossInhibitor = Math.Clamp(1-(SMcomponent.PowerlossDynamicScaling * Math.Clamp(moles/SupermatterComponent.PowerlossInhibitionMoleBoostThreshold, 1f, 1.5f)), 0f, 1f);

                    //with a GasmixPowerRatio > 0.8, make the power more based on heat
                    //in normal mode (< 0.8), power is less effected by heat
                    //TODO: change pointlight brightness based off power ratio/temp factor
                    float TempFactor = SMcomponent.GasmixPowerRatio > 0.8 ? 50f : 30f;

                    //power is set for radiation
                    SMcomponent.Power = Math.Max((((temp * TempFactor) / Atmospherics.T0C) * SMcomponent.GasmixPowerRatio) + SMcomponent.Power, 0);

                    //more math to actually calculate radiation output
                    //rad.RadsPerSecond = SMcomponent.Power * Math.Max(0f, (1f + (SMcomponent.PowerTransmissionBonus/10f)));
                    //_radiationSystem.Radiate(SMcomponent.Power * Math.Max(0f, (1f + (SMcomponent.PowerTransmissionBonus/10f))), 10f, SMcomponent, frameTime);

                    //TODO: PsyCoeff should change from 0-1 based on psycologist distance
                    float energy = SMcomponent.Power * SupermatterComponent.ReactionPowerModefier * (1f - (SMcomponent.PsyCoeff * 0.2f));

                    //the following comment is tgspeak, there is a non zero chance that it is compltely invalid for ss14

                    //To figure out how much temperature to add each tick, consider that at one atmosphere's worth
                    //of pure oxygen, with all four lasers firing at standard energy and no N2 present, at room temperature
                    //that the device energy is around 2140. At that stage, we don't want too much heat to be put out
                    //Since the core is effectively "cold"

                    //Also keep in mind we are only adding this temperature to (efficiency)% of the one tile the rock
                    //is on. An increase of 4*C @ 25% efficiency here results in an increase of 1*C / (#tilesincore) overall.
                    //Power * 0.55 * (some value between 1.5 and 23) / 5

                    temp += ((energy * SMcomponent.DynamicHeatModifier) / Atmospherics.ThermalReleaseModifier);

                    //We can only emit so much heat, that being 57500
                    temp = Math.Max(0, Math.Min(temp, 2500 * SMcomponent.DynamicHeatModifier));

                    //Calculate how much gas to release
                    //Varies based on power and gas content
                    Pla += Math.Max(((energy * SMcomponent.DynamicHeatModifier) / Atmospherics.PlasmaReleaseModifier), 0f);
                    //Varies based on power, gas content, and heat
                    Oxy += Math.Max((((energy + temp * SMcomponent.DynamicHeatModifier) - Atmospherics.T0C) / Atmospherics.OxygenReleaseModifier), 0f);

                    //i honestly have no idea what this is doing
                    float FuckMeUp = (float) Math.Pow(SMcomponent.Power / 500f, 3f);

                    //for supermatter soothing
                    float PsyEffect = (1f - (0.2f * SMcomponent.PsyCoeff));

                    //final power calculation for lowering power
                    SMcomponent.Power = Math.Clamp(SMcomponent.Power - Math.Min(FuckMeUp * SMcomponent.PowerlossInhibitor, SMcomponent.Power * 0.83f * SMcomponent.PowerlossInhibitor) * PsyEffect, 0f, 10000f);
                }
            }
        }

        /// </summary>
        /// Handles environmental damage and dispatching damage warning
        /// </summary>
        public void HandleDamage(EntityUid Uid, float frameTime, SupermatterComponent? SMcomponent = null, DamageableComponent? damageable = null, TransformComponent? Xform = null, ExplosiveComponent? Xplode = null)
        {
            if (!Resolve(Uid, ref SMcomponent, ref damageable, ref Xform, ref Xplode))
            {
                return;
            }

            SMcomponent.DamageUpdateAccumulator += frameTime;

            if (SMcomponent.DamageUpdateAccumulator > SupermatterComponent.DamageUpdateTimer)
            {
                float damage = 0;

                SMcomponent.DamageArchived = damageable.TotalDamage.Float();

                //gets the integrity as a percentage
                var integrity = (100 - (100 * (damageable.TotalDamage.Float() / SupermatterComponent.ExplosionPoint)));

                if (SMcomponent.DamageArchived >= SupermatterComponent.ExplosionPoint)
                {
                    Delamination(Uid, frameTime, SMcomponent, Xform, Xplode);
                    return;
                }
                else
                {
                    SMcomponent.YellAccumulator += frameTime;
                    if (SMcomponent.YellAccumulator >= SupermatterComponent.YellTimer)
                    {
                        if (SMcomponent.DamageArchived >= SupermatterComponent.EmergencyPoint && SMcomponent.DamageArchived <= SupermatterComponent.ExplosionPoint)
                        {
                            _chatManager.DispatchStationAnnouncement(Loc.GetString("supermatter-warning-message", ("integrity", integrity.ToString("0.00"))), "Supermatter", false);
                            SMcomponent.YellAccumulator = 0;
                        }
                        if (SMcomponent.DamageArchived >= SupermatterComponent.WarningPoint && SMcomponent.DamageArchived <= SupermatterComponent.EmergencyPoint)
                        {
                            var messageWrap = Loc.GetString("chat-manager-send-admin-announcement-wrap-message",
                            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")));
                            _chat.TrySendInGameICMessage(Uid, Loc.GetString("supermatter-danger-message", ("integrity", integrity.ToString("0.00"))), InGameICChatType.Speak, false);
                            SMcomponent.YellAccumulator = 0;
                        }
                    }
                }

                //if in space
                if (!Xform.GridID.IsValid())
                {
                    damage = Math.Max((SMcomponent.Power / 1000) * SupermatterComponent.DamageIncreaseMultiplier, 0.1f);
                }

                //if in an atmosphere
                if (_atmosphere.GetTileMixture(Xform.Coordinates) is { } mixture)
                {
                    //((((some value between 0.5 and 1 * temp - ((273.15 + 40) * some values between 1 and 10)) * some number between 0.25 and knock your socks off / 150) * 0.25
                    //Heat and mols account for each other, a lot of hot mols are more damaging then a few
                    //Mols start to have a positive effect on damage after 350
                    var MoleClamp = Math.Clamp(mixture.TotalMoles / 200f, 0.5f, 1f);
                    var HeatDamage = ((Atmospherics.T0C + SupermatterComponent.HeatPenaltyThreshold)*SMcomponent.DynamicHeatResistance);
                    damage = Math.Max(damage + (Math.Max(MoleClamp * mixture.Temperature - HeatDamage, 0) * SMcomponent.MoleHeatPenalty / 150) * SupermatterComponent.DamageIncreaseMultiplier, 0f);

                    //Power only starts affecting damage when it is above 5000
                    damage = Math.Max(damage + (Math.Max(SMcomponent.Power - SupermatterComponent.PowerPenaltyThreshold, 0f)/500f) * SupermatterComponent.DamageIncreaseMultiplier, 0f);

                    //Molar count only starts affecting damage when it is above 1800
                    damage = Math.Max(damage + (Math.Max(mixture.TotalMoles - SupermatterComponent.MolePenaltyThreshold, 0f)/80f) * SupermatterComponent.DamageIncreaseMultiplier, 0f);

                    //There might be a way to integrate healing and hurting via heat
			        //healing damage
                    if (mixture.TotalMoles < SupermatterComponent.MolePenaltyThreshold)
                    {
                        //Only has a net positive effect when the temp is below 313.15, heals up to 2 damage. Psycologists increase this temp min by up to 45
                        var heatcap = ((Atmospherics.T0C + SupermatterComponent.HeatPenaltyThreshold) + (45 * SMcomponent.PsyCoeff));
                        damage = Math.Max(-(2 * (1 - (mixture.Temperature / heatcap))),-2);
                    }

                    //if there are space tiles next to SM
                    //TODO: change moles out for checking if adjacent tiles exist
                    foreach(var adjacent in _atmosphere.GetAdjacentTileMixtures(Xform.Coordinates))
                    {
                        if (adjacent.TotalMoles == 0)
                        {
                            float factor;

                            factor = integrity switch
                            {
                                (<= 25) => 0.002f,
                                (<= 55) and (> 25) => 0.005f,
                                (<= 75) and (> 55) => 0.0009f,
                                (<= 90) and (> 75) => 0.0005f,
                                _ => 0,
                            };

                            damage = Math.Clamp((SMcomponent.Power * factor) * SupermatterComponent.DamageIncreaseMultiplier, 0f, SupermatterComponent.MaxSpaceExposureDamage);
                        }
                    }
                }

                //only take up to 1.8 damage per cycle with no lower limmit
                damage = Math.Min(SMcomponent.DamageArchived + (SupermatterComponent.DamageHardcap * SupermatterComponent.ExplosionPoint), damage);
                //damage to add to total
                var damageDelta = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), (Shared.FixedPoint.FixedPoint2)damage);
                _damageable.TryChangeDamage(SMcomponent.Owner, damageDelta, true);

                SMcomponent.DamageUpdateAccumulator -= SupermatterComponent.DamageUpdateTimer;
            }
        }

        /// </summary>
        /// Runs the logic and timers for Delamination
        /// </summary>
        public void Delamination(EntityUid Uid, float frameTime, SupermatterComponent? SMcomponent = null, TransformComponent? Xform = null, ExplosiveComponent? Xplode = null)
        {
            if (!Resolve(Uid, ref SMcomponent, ref Xform, ref Xplode))
            {
                return;
            }

            var _delamType = SupermatterComponent.DelamType.Explosion;

            var audioParams = AudioParams.Default;
            audioParams.Loop = true;
            audioParams.MaxDistance = 20f;
            audioParams.Volume = 5;

            //before we actually start counting down, check to see what delam type we're doing.
            if (!SMcomponent.FinalCountdown)
            {
                //if we're in atmos
                if (_atmosphere.GetTileMixture(Xform.Coordinates) is { } mixture)
                {
                    //if the moles on the sm's tile are above MolePenaltyThreshold
                    if (mixture.TotalMoles >= SupermatterComponent.MolePenaltyThreshold)
                    {
                        _delamType = SupermatterComponent.DelamType.Singulo;
                        _chatManager.DispatchStationAnnouncement(Loc.GetString("supermatter-delamination-overmass"), "Supermatter", false);
                    }
                }
                else
                {
                    _delamType = SupermatterComponent.DelamType.Explosion;
                    _chatManager.DispatchStationAnnouncement(Loc.GetString("supermatter-delamination-default"), "Supermatter", false);
                }
            }

            SMcomponent.FinalCountdown = true;

            SMcomponent.DelamTimerAccumulator += frameTime;
            SMcomponent.SpeakAccumulator += frameTime;
            int RoundSeconds = SupermatterComponent.DelamTimerTimer - (int)Math.Floor(SMcomponent.DelamTimerAccumulator);

            //we healed out of delam, return
            if (SMcomponent.DamageArchived < SupermatterComponent.ExplosionPoint)
            {
                _chatManager.DispatchStationAnnouncement(Loc.GetString("supermatter-safe-allert"), "Supermatter", false);
                SMcomponent.FinalCountdown = false;
                return;
            }
            //we're more than 5 seconds from delam, only yell every 5 seconds.
            else if (RoundSeconds >= 5 && SMcomponent.SpeakAccumulator >= 5)
            {
                SMcomponent.SpeakAccumulator -= 5;
                _chatManager.DispatchStationAnnouncement(Loc.GetString("supermatter-seconds-before-delam", ("Seconds", RoundSeconds)), "Supermatter", false);
            }
            //less than 5 seconds to delam, count every second.
            else if (RoundSeconds <  5 && SMcomponent.SpeakAccumulator >= 1)
            {
                SMcomponent.SpeakAccumulator -= 1;
                _chatManager.DispatchStationAnnouncement(Loc.GetString("supermatter-seconds-before-delam", ("Seconds", RoundSeconds)), "Supermatter", false);
            }

            //play an alarm as long as you're delaming
            if (SMcomponent.FinalCountdown)
            {
                SoundSystem.Play(Filter.Pvs(Uid, 5), SMcomponent.DelamAlarm.GetSound(), Uid, audioParams);
            }

            //TODO: make tesla(?) spawn at SupermatterComponent.PowerPenaltyThreshold and think up other delam types
            //times up, explode or make a singulo
            if (SMcomponent.DelamTimerAccumulator >= SupermatterComponent.DelamTimerTimer)
            {
                if (_delamType.Equals(SupermatterComponent.DelamType.Singulo))
                {
                    //spawn a singulo :)
                    EntityManager.SpawnEntity("Singularity", Xform.Coordinates);
                }
                else if (_delamType.Equals(SupermatterComponent.DelamType.Explosion))
                {
                    //esplosion!!!!!
                    _explosion.TriggerExplosive(Uid, explosive: Xplode, totalIntensity: 500000, radius: 500, user: Uid);
                }

                SMcomponent.FinalCountdown = false;
                return;
            }
        }

        /// </summary>
        /// Determines if an entity can be dusted
        /// </summary>
        public bool CannotDestroy(EntityUid Uid)
        {
            bool Tag = false;
            bool Static = false;

            if (EntityManager.HasComponent<TagComponent>(Uid))
            {
                Tag = _tag.HasTag(Uid, "SMimmune");
            }

            if (EntityManager.HasComponent<PhysicsComponent>(Uid))
            {
                Static = EntityManager.GetComponent<PhysicsComponent>(Uid).BodyType.ToString() == "Static";
            }

            return Tag || Static;
        }

        private void OnCollideEvent(EntityUid uid, SupermatterComponent supermatter, StartCollideEvent args)
        {
            EntityUid Target = args.OtherFixture.Body.Owner;
            if (!supermatter.Whitelist.IsValid(Target) || CannotDestroy(Target) || _container.IsEntityInContainer(uid)) return;

            if (EntityManager.TryGetComponent<SupermatterFoodComponent>(Target, out var SupermatterFood))
            {
                supermatter.Power += SupermatterFood.Energy;
            }
            else if (EntityManager.TryGetComponent<ProjectileComponent>(Target, out var Projectile))
            {
                supermatter.Power += (float) Projectile.Damage.Total;
            }
            else
            {
                supermatter.Power++;
            }

            if (!EntityManager.HasComponent<ProjectileComponent>(Target))
            {
                EntityManager.SpawnEntity("Ash", Transform(Target).Coordinates);
            }

            EntityManager.QueueDeleteEntity(Target);
        }
    }
}
