using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs.Body;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Mobs
{
    public class MobComponent : SharedMobComponent, IActionBlocker, IOnDamageBehavior, IOnDamageReceived, IExAct
    {
#pragma warning disable CS0649
        [Dependency]
        protected IPrototypeManager PrototypeManager;
#pragma warning restore
        /// <summary>
        /// Damagestates are reached by reaching a certain damage threshold, they will block actions after being reached
        /// </summary>
        public DamageState CurrentDamageState { get; private set; } = new NormalState();

        /// <summary>
        /// Damage state enum for current health, set only via change damage state //TODO: SETTER
        /// </summary>
        private ThresholdType currentstate = ThresholdType.None;

        /// <summary>
        /// Holds the damage template which controls the threshold and resistance settings for this mob type
        /// </summary>
        private DamageTemplates DamageTemplate;

        /// <summary>
        /// Holds the body template which controls the organs, body functions and processes in living beings 
        /// </summary>
        public BodyTemplate Body { get; private set; }

        public List<string> States;

        /// <summary>
        /// Variable for serialization
        /// </summary>
        private string templatename;
        private string bodyTemplateName;

        private int _heatResistance;
        public int HeatResistance => _heatResistance;

        private int _lifeTick = 0;

        Random _seed;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref templatename, "damageTemplate", "Human");

            Type type = AppDomain.CurrentDomain.GetAssemblyByName("Content.Server")
                .GetType("Content.Server.GameObjects." + templatename);
            DamageTemplate = (DamageTemplates) Activator.CreateInstance(type);

            serializer.DataField(ref bodyTemplateName, "bodyTemplate", "Human");

            if (PrototypeManager.TryIndex<BodyTemplate>(bodyTemplateName, out var body))
            {
                Body = body;
                Body.Initialize(Owner);
            }
            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public override void Initialize()
        {
            base.Initialize();
            PrototypeManager = IoCManager.Resolve<IPrototypeManager>();
            _seed = new Random(Owner.Uid.GetHashCode() ^ DateTime.Now.GetHashCode());
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    var list = Body.RenderDoll();
                    var hudstatechange = DamageTemplate.ChangeHudState(list, Owner.GetComponent<DamageableComponent>());
                    SendNetworkMessage(hudstatechange);
                    break;
            }
        }

        bool IActionBlocker.CanMove()
        {
            return CurrentDamageState.CanMove();
        }

        bool IActionBlocker.CanInteract()
        {
            return CurrentDamageState.CanInteract();
        }

        bool IActionBlocker.CanUse()
        {
            return CurrentDamageState.CanUse();
        }

        List<DamageThreshold> IOnDamageBehavior.GetAllDamageThresholds()
        {
            var thresholdlist = DamageTemplate.DamageThresholds;
            thresholdlist.AddRange(DamageTemplate.HealthHudThresholds);
            return thresholdlist;
        }

        void IOnDamageReceived.OnDamageReceived(OnDamageReceivedEventArgs e)
        {
            //limb/organ damage
            if (Body != null) //this event gets called twice, for Total too, and we don't need it tbh
            {
                Body.HandleDamage(e.DamageType, e.Damage);
            }
        }

        void IOnDamageBehavior.OnDamageThresholdPassed(object damageable, DamageThresholdPassedEventArgs e)
        {
            DamageableComponent damage = (DamageableComponent) damageable;

            if (e.DamageThreshold.ThresholdType != ThresholdType.HUDUpdate)
            {
                ChangeDamageState(DamageTemplate.CalculateDamageState(damage));

            }

            if (Owner.TryGetComponent(out BasicActorComponent actor)
            ) //specifies if we have a client to update the hud for
            {
                var list = Body.RenderDoll();
                var hudstatechange = DamageTemplate.ChangeHudState(list, damage);
                SendNetworkMessage(hudstatechange);
            }
        }

        private void ChangeDamageState(ThresholdType threshold)
        {
            if (threshold == currentstate)
            {
                return;
            }

            CurrentDamageState.ExitState(Owner);
            CurrentDamageState = DamageTemplates.StateThresholdMap[threshold];
            CurrentDamageState.EnterState(Owner);

            currentstate = threshold;

            Owner.RaiseEvent(new MobDamageStateChangedMessage(this));
        }

        public void OnUpdate()
        {
            Body.Life(_lifeTick);
            _lifeTick++;
        }


        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var burnDamage = 0;
            var bruteDamage = 0;
            switch(eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    DestroyOwner();
                    break;
                case ExplosionSeverity.Heavy:
                    if (_seed.Prob(0.6f))
                    {
                        DestroyOwner();
                    }
                    else
                    {
                        bruteDamage += 60;
                        burnDamage += 60;
                    }
                    break;
                case ExplosionSeverity.Light:
                    bruteDamage += 30;
                    break;
            }
            if (bruteDamage > 0)
            {
                Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Brute, bruteDamage);
            }
            if (burnDamage > 0)
            {
                Owner.GetComponent<DamageableComponent>().TakeDamage(DamageType.Heat, burnDamage);
            }
        }

        private void DestroyOwner()
        {
            Body.Gib();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.ForceSpawnEntityAt("MobObserver", Owner.Transform.GridPosition);
            var mind = Owner.GetComponent<MindComponent>().Mind;
            mind.UnVisit();
            mind.Visit(ghost);
            Owner.Delete();
        }
    }

    /// <summary>
    ///     Fired when <see cref="MobComponent.CurrentDamageState"/> changes.
    /// </summary>
    public sealed class MobDamageStateChangedMessage : EntitySystemMessage
    {
        public MobDamageStateChangedMessage(MobComponent mob)
        {
            Mob = mob;
        }

        /// <summary>
        ///     The mob component that was changed.
        /// </summary>
        public MobComponent Mob { get; }
    }
}
