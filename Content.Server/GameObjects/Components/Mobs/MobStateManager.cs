using System.Collections.Generic;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Damage;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     When attacked to an <see cref="BaseDamageableComponent"/>, this component will handle critical and death behaviors
    ///     for mobs.
    ///     Additionally, it handles sending effects to clients (such as blur effect for unconsciousness) and managing the
    ///     health HUD.
    /// </summary>
    [RegisterComponent]
    internal class MobStateManagerComponent : Component, IOnHealthChangedBehavior, IActionBlocker
    {
        private readonly Dictionary<DamageState, IMobState> _behavior = new Dictionary<DamageState, IMobState>
        {
            {DamageState.Alive, new NormalState()},
            {DamageState.Critical, new CriticalState()},
            {DamageState.Dead, new DeadState()}
        };

        private DamageState _currentDamageState;
        public override string Name => "MobStateManager";

        public IMobState CurrentMobState { get; private set; } = new NormalState();


        bool IActionBlocker.CanInteract()
        {
            return CurrentMobState.CanInteract();
        }

        bool IActionBlocker.CanMove()
        {
            return CurrentMobState.CanMove();
        }

        bool IActionBlocker.CanUse()
        {
            return CurrentMobState.CanUse();
        }

        bool IActionBlocker.CanThrow()
        {
            return CurrentMobState.CanThrow();
        }

        bool IActionBlocker.CanSpeak()
        {
            return CurrentMobState.CanSpeak();
        }

        bool IActionBlocker.CanDrop()
        {
            return CurrentMobState.CanDrop();
        }

        bool IActionBlocker.CanPickup()
        {
            return CurrentMobState.CanPickup();
        }

        bool IActionBlocker.CanEmote()
        {
            return CurrentMobState.CanEmote();
        }

        bool IActionBlocker.CanAttack()
        {
            return CurrentMobState.CanAttack();
        }

        bool IActionBlocker.CanEquip()
        {
            return CurrentMobState.CanEquip();
        }

        bool IActionBlocker.CanUnequip()
        {
            return CurrentMobState.CanUnequip();
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return CurrentMobState.CanChangeDirection();
        }

        public void OnHealthChanged(HealthChangedEventArgs e)
        {
            if (e.DamageableComponent.CurrentDamageState != _currentDamageState)
            {
                _currentDamageState = e.DamageableComponent.CurrentDamageState;
                CurrentMobState.ExitState(Owner);
                CurrentMobState = _behavior[_currentDamageState];
                CurrentMobState.EnterState(Owner);
            }

            CurrentMobState.UpdateState(Owner);
        }

        public override void Initialize()
        {
            base.Initialize();

            _currentDamageState = DamageState.Alive;
            CurrentMobState = _behavior[_currentDamageState];
            CurrentMobState.EnterState(Owner);
            CurrentMobState.UpdateState(Owner);
        }

        public override void OnRemove()
        {
            // TODO: Might want to add an OnRemove() to IMobState since those are where these components are being used
            base.OnRemove();

            Owner.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent);
            statusEffectsComponent?.RemoveStatusEffect(StatusEffect.Health);

            Owner.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent);
            overlayEffectsComponent?.ClearOverlays();
        }
    }

    /// <summary>
    ///     Defines the blocking effects of an associated <see cref="DamageState"/> (i.e. Normal, Crit, Dead) and what effects
    ///     to apply upon entering or exiting the state.
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
        public void EnterState(IEntity entity)
        {
            UpdateState(entity);
        }

        public void ExitState(IEntity entity)
        {
        }

        public void UpdateState(IEntity entity)
        {
            // Setup HUD icon
            if (!entity.TryGetComponent(out ServerStatusEffectsComponent statusEffectsComponent))
            {
                return;
            }

            if (entity.TryGetComponent(out BaseDamageableComponent damageableComponent))
            {
                if (damageableComponent is BasicRuinableComponent basicRuinableComponent)
                {
                    if (basicRuinableComponent.TotalDamage > basicRuinableComponent.MaxHP)
                    {
                        statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                            "/Textures/Interface/StatusEffects/Human/humandead.png");
                    }
                    else
                    {
                        var modifier =
                            (int) (basicRuinableComponent.TotalDamage / (basicRuinableComponent.MaxHP / 7f));
                        statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                            "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");
                    }
                }
                else if (damageableComponent is BodyManagerComponent bodyManagerComponent)
                {
                    //Temporary 10 hits = die system
                    if (bodyManagerComponent.TempDamageThing >= 10)
                    {
                        statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                            "/Textures/Interface/StatusEffects/Human/humandead.png");
                    }
                    else
                    {
                        var modifier = (int) (bodyManagerComponent.TempDamageThing / (10f / 7f));
                        statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                            "/Textures/Interface/StatusEffects/Human/human" + modifier + ".png");
                    }
                }
                else
                {
                    statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                        "/Textures/Interface/StatusEffects/Human/human0.png");
                }
            }
            else
            {
                statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                    "/Textures/Interface/StatusEffects/Human/human0.png");
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
            {
                statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                    "/Textures/Interface/StatusEffects/Human/humancrit-0.png"); //Todo: combine humancrit-0 and humancrit-1 into a gif and display it
            }

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
            {
                overlayComponent.AddOverlay(SharedOverlayID.GradientCircleMaskOverlay);
            }

            if (entity.TryGetComponent(out StunnableComponent stun))
            {
                stun.CancelAll();
            }

            StandingStateHelper.Down(entity);
        }

        public void ExitState(IEntity entity)
        {
            StandingStateHelper.Standing(entity);

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
            {
                overlayComponent.ClearOverlays();
            }
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
            {
                statusEffectsComponent.ChangeStatusEffectIcon(StatusEffect.Health,
                    "/Textures/Interface/StatusEffects/Human/humandead.png");
            }

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
            {
                overlayComponent.AddOverlay(SharedOverlayID.CircleMaskOverlay);
            }

            if (entity.TryGetComponent(out StunnableComponent stun))
            {
                stun.CancelAll();
            }

            StandingStateHelper.Down(entity);

            if (entity.TryGetComponent(out CollidableComponent collidable))
            {
                collidable.CanCollide = false;
            }
        }

        public void ExitState(IEntity entity)
        {
            StandingStateHelper.Standing(entity);

            if (entity.TryGetComponent(out CollidableComponent collidable))
            {
                collidable.CanCollide = true;
            }

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayComponent))
            {
                overlayComponent.ClearOverlays();
            }
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
