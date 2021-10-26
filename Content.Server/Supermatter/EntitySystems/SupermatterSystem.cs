using System;
using System.Data;
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

namespace Content.Server.SupermatterSystem{
    [UsedImplicitly]

    public class SupermatterSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        private RadioSystem _radioSystem = default!;

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
        public void HandleRads(SupermatterComponent component, IEntity entity)
        {
            if (_atmosphereSystem.GetTileMixture(component.Owner.Transform.Coordinates) is { } mixture)
                {
                    component.Mix = mixture;
                    var pressure = mixture.Pressure;
                    var moles = mixture.TotalMoles;
                    var Oxy = mixture.Moles[0];
                    component.GasComp[0] = Math.Clamp(mixture.Moles[0] / mixture.TotalMoles, 0, 1);

                    var Nit = mixture.Moles[1];
                    component.GasComp[1] = Math.Clamp(mixture.Moles[1] / mixture.TotalMoles, 0, 1);

                    var Co2 = mixture.Moles[2];
                    component.GasComp[2] = Math.Clamp(mixture.Moles[2] / mixture.TotalMoles, 0, 1);

                    var pla = mixture.Moles[3];
                    component.GasComp[3] = Math.Clamp(mixture.Moles[3] / mixture.TotalMoles, 0, 1);

                    var tri = mixture.Moles[4];
                    component.GasComp[4] = Math.Clamp(mixture.Moles[4] / mixture.TotalMoles, 0, 1);

                    var h2o = mixture.Moles[5];
                    component.GasComp[5] = Math.Clamp(mixture.Moles[5] / mixture.TotalMoles, 0, 1);

                    var h2oBonus = 1 - (component.GasComp[5] * 0.25f);

                    component.GasmixPowerRatio = 0f;
                    foreach (int gasId in component.GasPowermix)
                    {
                        component.GasmixPowerRatio += component.GasComp[gasId] * component.GasPowermix[gasId];
                    };
                    component.GasmixPowerRatio = Math.Clamp(component.GasmixPowerRatio, 0, 1);

                }
        }

        public bool CanDestroy(IEntity entity)
        {
            return entity.HasComponent<SharedBodyComponent>() || entity.HasComponent<SharedItemComponent>();
        }

        public bool CantDestroy(IEntity entity)
        {
            return entity.MetaData.EntityName == "ash" || entity.IsInContainer() ||
            entity.HasComponent<GhostComponent>() || entity.HasComponent<SubFloorHideComponent>();
        }

        public void HandleDamage(SupermatterComponent component, IEntity entity)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            if (entity.HasComponent<SupermatterComponent>())
            {
                if (component.Damage >= 900)
                {
                entity.SpawnExplosion(25, 25, 30, 30); //TODO: Tune This Once Pow3er Works and put it into its own function
                entity.QueueDelete();
                return;
                }
                else if (component.Damage >= 700)
                {
                    component.emergency_point = true;
                    //TODO: yell over comms about blowing the fuck up
                }
                else if (component.Damage >= 50)
                {
                    component.warning_point = true;
                    chat.DispatchStationAnnouncement("Danger! Crystal hyperstructure integrity faltering!", "Supermatter");
                    //TODO: yell over comms about being bullied
                }
            }

        }
        public void HandleDestroy(SupermatterComponent component, IEntity entity){
            // TODO: Need SM immune tag for ash
            if (!CanDestroy(entity) || CantDestroy(entity)) return;

            EntityManager.SpawnEntity("Ash", entity.Transform.MapPosition);
            entity.QueueDelete();

            if (entity.TryGetComponent<SupermatterFoodComponent>(out var SupermatterFood)){
                component.Energy += SupermatterFood.Energy;
            }
            else if (entity.TryGetComponent<ProjectileComponent>(out var Projectile)){
                component.Energy += Projectile.Damage.Total;
            }
            else{
                component.Energy++;
            }
        }

        /// <summary>
        /// Handle getting entities in range and calling behavour
        /// </summary>
        public void HandleBehavour(SupermatterComponent component, Vector2 worldPos){
            foreach (var entity in _lookup.GetEntitiesInRange(component.Owner.Transform.MapID, worldPos, 0.5f)){
                HandleDestroy(component, entity);
                //HandleDamage(component, entity);
                HandleRads(component, entity);
            }
        }

        public void Update(SupermatterComponent component, float frameTime){
            var worldPos = component.Owner.Transform.WorldPosition;
            HandleBehavour(component, worldPos);
        }
    }
}
