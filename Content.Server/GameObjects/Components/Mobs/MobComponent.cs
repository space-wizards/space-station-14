using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs.Body;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Reflection;

namespace Content.Server.GameObjects.Components.Mobs
{
    public class MobComponent : SharedMobComponent, IActionBlocker, IOnDamageBehavior, IOnDamageReceived, IExAct
    {
#pragma warning disable CS0649
        [Dependency]
        protected IReflectionManager reflectionManager;
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

        public List<string> States;

        /// <summary>
        /// Variable for serialization
        /// </summary>
        private string templatename;

        private int _heatResistance;
        public int HeatResistance => _heatResistance;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref templatename, "damageTemplate", "Human");

            Type newtype = reflectionManager.GetType("Content.Server.GameObjects." + templatename);
            DamageTemplate = (DamageTemplates) Activator.CreateInstance(newtype);

            serializer.DataFieldCached(ref _heatResistance, "HeatResistance", 323);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    var list = new List<LimbRender>();
                    if (Owner.TryGetComponent(out BodyComponent body))
                    {
                        list = body.Body.RenderDoll();
                    }
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
                var list = new List<LimbRender>();
                if (Owner.TryGetComponent(out BodyComponent body))
                {
                    list = body.Body.RenderDoll(); //we need to render limbs!
                }

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

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {

        }

        void IOnDamageReceived.OnDamageReceived(OnDamageReceivedEventArgs e)
        {
            if (Owner.TryGetComponent(out BasicActorComponent actor)
            ) //specifies if we have a client to update the hud for
            {
                var list = new List<LimbRender>();
                if (Owner.TryGetComponent(out BodyComponent body))
                {
                    list = body.Body.RenderDoll(); //we need to render limbs!
                }

                var hudstatechange = DamageTemplate.ChangeHudState(list, Owner.GetComponent<DamageableComponent>());
                SendNetworkMessage(hudstatechange);
            }
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
    [Serializable]
    public enum AttackTargetDef
    {
        Head,
        Eyes,
        Mouth,
        Chest,
        LeftArm,
        RightArm,
        LeftHand,
        RightHand,
        Groin,
        LeftLeg,
        RightLeg,
        LeftFoot,
        RightFoot,
        SeveralTargets,
        All
    }
}
