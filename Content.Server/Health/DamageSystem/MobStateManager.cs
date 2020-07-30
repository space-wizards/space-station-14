using Content.Server.BodySystem;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.DamageSystem
{
    /// <summary>
    ///     When attacked to an <see cref="IDamageableComponent"/>, this component will handle critical and death behaviors for mobs.
    ///     Additionally, it handles sending effects to clients (such as blur effect for unconsciousness) and managing the health HUD.
    /// </summary>
    [RegisterComponent]
    class MobStateManagerComponent : Component, IOnHealthChangedBehavior, IActionBlocker
    {
        public override string Name => "MobStateManager";

        private Dictionary<DamageState, IMobState> _behavior = new Dictionary<DamageState, IMobState>() {
            { DamageState.Alive, new NormalState() },
            { DamageState.Critical, new CriticalState() },
            { DamageState.Dead, new DeadState() },
        };

        private DamageState _currentDamageState;
        private IMobState _currentMobState = new NormalState();

        public override void Initialize()
        {
            base.Initialize();
            _currentDamageState = DamageState.Alive;;
            _currentMobState = _behavior[_currentDamageState];
            _currentMobState.EnterState(Owner);
            _currentMobState.UpdateState(Owner);
        }

        public void OnHealthChanged(HealthChangedEventArgs e)
        {
            if (e.DamageableComponent.CurrentDamageState != _currentDamageState)
            {
                _currentDamageState = e.DamageableComponent.CurrentDamageState;
                _currentMobState.ExitState(Owner);
                _currentMobState = _behavior[_currentDamageState];
                _currentMobState.EnterState(Owner);
            }
            _currentMobState.UpdateState(Owner);
        }

        public override void OnRemove()
        {
            //Might want to add an OnRemove() to IMobState since those are where these components are being used
            base.OnRemove();
            Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
            statusEffectsComponent?.RemoveStatusEffect(StatusEffect.Health);

            Owner.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent);
            overlayEffectsComponent?.ClearOverlays();
        }


        bool IActionBlocker.CanInteract()
        {
            return _currentMobState.CanInteract();
        }
        bool IActionBlocker.CanMove()
        {
            return _currentMobState.CanMove();
        }
        bool IActionBlocker.CanUse()
        {
            return _currentMobState.CanUse();
        }
        bool IActionBlocker.CanThrow()
        {
            return _currentMobState.CanThrow();
        }
        bool IActionBlocker.CanSpeak()
        {
            return _currentMobState.CanSpeak();
        }
        bool IActionBlocker.CanDrop()
        {
            return _currentMobState.CanDrop();
        }
        bool IActionBlocker.CanPickup()
        {
            return _currentMobState.CanPickup();
        }
        bool IActionBlocker.CanEmote()
        {
            return _currentMobState.CanEmote();
        }
        bool IActionBlocker.CanAttack()
        {
            return _currentMobState.CanAttack();
        }
        bool IActionBlocker.CanEquip()
        {
            return _currentMobState.CanEquip();
        }
        bool IActionBlocker.CanUnequip()
        {
            return _currentMobState.CanUnequip();
        }
        bool IActionBlocker.CanChangeDirection()
        {
            return _currentMobState.CanChangeDirection();
        }
    }



    /// <summary>
    ///     Defines the blocking effects of an associated <see cref="DamageState"/> (i.e. Normal, Crit, Dead) and what effects to apply upon entering or exiting the state.
    /// </summary>
    public interface IMobState : IActionBlocker
    {
        /// <summary>
        ///     Called when this state is entered.
        /// </summary>
        void EnterState(IEntity entity);

        /// <summary>
        ///     Called when this state is left for a different state.
        /// </summary>
        void ExitState(IEntity entity);

        /// <summary>
        ///     Called when this state is updated.
        /// </summary>
        void UpdateState(IEntity entity);

    }

    /// <summary>
    ///     The standard state an entity is in; no negative effects.
    /// </summary>
    public struct NormalState : IMobState
    {
        public void EnterState(IEntity entity) {
            UpdateState(entity);
        }

        public void ExitState(IEntity entity) {

        }

        public void UpdateState(IEntity entity)
        {
            if (entity.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent)) //Setup HUD icon
            {
                if (entity.TryGetComponent(out IDamageableComponent damageableComponent))
                {
                    if (damageableComponent is BasicRuinableComponent)
                    {
                        BasicRuinableComponent basicRuinableComponent = damageableComponent as BasicRuinableComponent;
                        if (basicRuinableComponent.TotalDamage > basicRuinableComponent.MaxHP)
                        {
                            statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/humandead.png");
                        }
                        else
                        {
                            var modifier = (int) ((float) basicRuinableComponent.TotalDamage / ((float) basicRuinableComponent.MaxHP / 7f));
                            statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");
                        }
                    }
                    else if (damageableComponent is BodyManagerComponent)
                    {
                        //Temporary 10 hits = die system
                        BodyManagerComponent bodyManagerComponent = damageableComponent as BodyManagerComponent;
                        if (bodyManagerComponent.TempDamageThing >= 10)
                        {
                            statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/humandead.png");
                        }
                        else
                        {
                            var modifier = (int) ((float) bodyManagerComponent.TempDamageThing / (10f / 7f));
                            statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");
                        }
                    }
                    else
                    {
                        statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/human0.png");
                    }

                }
                else
                {
                    statusEffectsComponent?.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/human0.png");
                }
            }
        }

        public bool IsConscious => true;

        bool IActionBlocker.CanInteract()
        {
            return true;
        }

        bool IActionBlocker.CanMove()
        {
            return true;
        }

        bool IActionBlocker.CanUse()
        {
            return true;
        }

        bool IActionBlocker.CanThrow()
        {
            return true;
        }

        bool IActionBlocker.CanSpeak()
        {
            return true;
        }

        bool IActionBlocker.CanDrop()
        {
            return true;
        }

        bool IActionBlocker.CanPickup()
        {
            return true;
        }

        bool IActionBlocker.CanEmote()
        {
            return true;
        }

        bool IActionBlocker.CanAttack()
        {
            return true;
        }

        bool IActionBlocker.CanEquip()
        {
            return true;
        }

        bool IActionBlocker.CanUnequip()
        {
            return true;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return true;
        }
    }

    /// <summary>
    ///     A state in which an entity is disabled from acting due to sufficient damage (considered unconscious).
    /// </summary>
    public struct CriticalState : IMobState
    {
        public void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent))
                statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/humancrit-0.png"); //Todo: combine humancrit-0 and humancrit-1 into a gif and display it

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
                overlayComponent.AddOverlay(OverlayType.GradientCircleMaskOverlay);

            if (entity.TryGetComponent(out StunnableComponent stun))
                stun.CancelAll();

            StandingStateHelper.Down(entity);
        }

        public void ExitState(IEntity entity)
        {
            StandingStateHelper.Standing(entity);

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
                overlayComponent.ClearOverlays();
        }

        public void UpdateState(IEntity entity)
        {

        }

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }

        bool IActionBlocker.CanThrow()
        {
            return false;
        }

        bool IActionBlocker.CanSpeak()
        {
            return false;
        }

        bool IActionBlocker.CanDrop()
        {
            return false;
        }

        bool IActionBlocker.CanPickup()
        {
            return false;
        }

        bool IActionBlocker.CanEmote()
        {
            return false;
        }

        bool IActionBlocker.CanAttack()
        {
            return false;
        }

        bool IActionBlocker.CanEquip()
        {
            return false;
        }

        bool IActionBlocker.CanUnequip()
        {
            return false;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return false;
        }
    }

    /// <summary>
    ///     The state representing a dead entity; allows for ghosting.
    /// </summary>
    public struct DeadState : IMobState
    {
        public void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent))
                statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health, "/Textures/Interface/StatusEffects/Human/humandead.png");

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
                overlayComponent.AddOverlay(OverlayType.CircleMaskOverlay);

            if (entity.TryGetComponent(out StunnableComponent stun))
                stun.CancelAll();

            StandingStateHelper.Down(entity);

            if (entity.TryGetComponent(out CollidableComponent collidable))
                collidable.CanCollide = false;
        }

        public void ExitState(IEntity entity)
        {
            StandingStateHelper.Standing(entity);

            if (entity.TryGetComponent(out CollidableComponent collidable))
            {
                collidable.CanCollide = true;
            }

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
                overlayComponent.ClearOverlays();
        }

        public void UpdateState(IEntity entity)
        {

        }

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }

        bool IActionBlocker.CanThrow()
        {
            return false;
        }

        bool IActionBlocker.CanSpeak()
        {
            return false;
        }

        bool IActionBlocker.CanDrop()
        {
            return false;
        }

        bool IActionBlocker.CanPickup()
        {
            return false;
        }

        bool IActionBlocker.CanEmote()
        {
            return false;
        }

        bool IActionBlocker.CanAttack()
        {
            return false;
        }

        bool IActionBlocker.CanEquip()
        {
            return false;
        }

        bool IActionBlocker.CanUnequip()
        {
            return false;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return false;
        }
    }
}
