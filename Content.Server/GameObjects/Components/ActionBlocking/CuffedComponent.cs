
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.Log;
using System.Linq;
using Content.Server.GameObjects.Components.Items.Storage;
using Robust.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class CuffedComponent : SharedCuffedComponent
    {
        /// <summary>
        /// Data class used to track the type of handcuff used on each set of hands.
        /// </summary>
        public class CuffConfig
        {
            /// <summary>
            /// How long it takes to uncuff the entity with the CuffedComponent
            /// </summary>
            [ViewVariables]
            public float UncuffTime { get; set; }

            /// <summary>
            /// How long it takes for the cuffed entity to break out of the CuffedComponent
            /// </summary>
            [ViewVariables]
            public float BreakoutTime { get; set; }

            /// <summary>
            /// The ID of the prototype that spawns when the CuffedComponent is removed
            /// </summary>
            [ViewVariables]
            public string PrototypeId { get; set; }

            /// <summary>
            /// The RSI file used by these handcuffs
            /// </summary>
            [ViewVariables]
            public string RSI { get; set; }

            /// <summary>
            /// The iconstate name from the RSI to be used by the handcuffs
            /// </summary>
            [ViewVariables]
            public string IconState { get; set; }

            /// <summary>
            /// The color to tint the overlay with
            /// </summary>
            [ViewVariables]
            public Color Color { get; set; }

            /// <summary>
            /// Sound file to play when uncuffing begins.
            /// </summary>
            [ViewVariables]
            public string StartUncuffSound { get; set; }

            /// <summary>
            /// Sound file to play when uncuffing ends.
            /// </summary>
            [ViewVariables]
            public string EndUncuffSound { get; set; }

            /// <summary>
            /// Sound file to play when breaking out of cuffs
            /// </summary>
            [ViewVariables]
            public string BreakoutSound { get; set; }
        }

#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
#pragma warning restore 649

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => _cuffConfigs.Count * 2;

        /// <summary>
        /// Every set of hands tracks its own handcuff config. This is used in cases where an entity with >2 hands is cuffed using different types of cuffs.
        /// </summary>
        [ViewVariables]
        private List<CuffConfig> _cuffConfigs;

        private bool _deleteThisFrame = false;
        private bool _dirtyThisFrame = false;
        private float _interactRange;
        private DoAfterSystem _doAfterSystem;
        private AudioSystem _audioSystem;
        private HandsComponent _hands;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
            _doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            _interactRange = SharedInteractionSystem.InteractionRange / 2;
            _cuffConfigs = new List<CuffConfig>();

            if (!Owner.TryGetComponent(out _hands))
            {
                Logger.Warning($"CuffedComponent was added to an entity that does not have a HandsComponent!");
                Owner.RemoveComponent<CuffedComponent>();
            }
        }

        public override ComponentState GetComponentState()
        {
            // there are 2 approaches i can think of to handle the handcuff overlay on players
            // 1 - make the current RSI the handcuff type that's currently active. all handcuffs on the player will appear the same.
            // 2 - allow for several different player overlays for each different cuff type.
            // approach #2 would be more difficult/time consuming to do and the payoff doesn't make it worth it.
            // right now we're doing approach #1.

            if (CuffedHandCount == 0)
            {
                return new CuffedComponentState(CuffedHandCount,
                    CanStillInteract,
                    "/Objects/Misc/handcuffs.rsi",
                    "body-overlay-2",
                    Color.White);
            }
            else
            {
                return new CuffedComponentState(CuffedHandCount,
                    CanStillInteract,
                    _cuffConfigs[_cuffConfigs.Count - 1].RSI,
                    $"{_cuffConfigs[_cuffConfigs.Count - 1].IconState}-{CuffedHandCount}",
                    _cuffConfigs[_cuffConfigs.Count - 1].Color);
                // the iconstate is formatted as blah-2, blah-4, blah-6, etc.
                // the number corresponds to how many hands are cuffed.
            }
        }

        /// <summary>
        /// Add a set of cuffs to an existing CuffedComponent.
        /// </summary>
        /// <param name="prototype"></param>
        public void AddNewCuffs(CuffConfig config)
        {
            _cuffConfigs.Add(config);
            CanStillInteract = _hands.Count > CuffedHandCount;

            UpdateStatusEffect();
            UpdateHeldItems();
            Dirty();
        }

        public void Update(float frameTime)
        {
            if (_deleteThisFrame && Owner.HasComponent<CuffedComponent>())
            {
                Owner.RemoveComponent<CuffedComponent>();
            }

            UpdateHandCount();
        }

        /// <summary>
        /// Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void UpdateHandCount() 
        {
            _dirtyThisFrame = false;

            while (CuffedHandCount > _hands.Count && CuffedHandCount > 0)
            {
                Logger.Warning("Need to remove some cuffs because too few hands");
                _dirtyThisFrame = true;

                var config = _cuffConfigs[_cuffConfigs.Count - 1];
                _cuffConfigs.Remove(config);
                
                Owner.EntityManager.SpawnEntity(config.PrototypeId, Owner.Transform.GridPosition);
            }

            if (CuffedHandCount == 0)
            {
                _deleteThisFrame = true;
            }
            else if (_dirtyThisFrame)
            {
                CanStillInteract = _hands.Count > CuffedHandCount;
                Dirty();
            }
        }

        /// <summary>
        /// Check how many items the user is holding and if it's more than the number of cuffed hands, drop some items.
        /// </summary>
        public void UpdateHeldItems()
        {
            var itemCount = _hands.GetAllHeldItems().Count();
            var freeHandCount = _hands.Count - CuffedHandCount;

            if (freeHandCount < itemCount)
            {
                foreach (ItemComponent item in _hands.GetAllHeldItems())
                {
                    if (freeHandCount < itemCount)
                    {
                        freeHandCount++;
                        _hands.Drop(item.Owner);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the status effect indicator on the HUD.
        /// </summary>
        private void UpdateStatusEffect()
        {
            if (Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                status.ChangeStatusEffectIcon(StatusEffect.Cuffed,
                    CanStillInteract ? "/Textures/Interface/StatusEffects/Handcuffed/Uncuffed.png" : "/Textures/Interface/StatusEffects/Handcuffed/Handcuffed.png");
            }
        }

        /// <summary>
        /// Attempt to uncuff a cuffed entity. Can be called by the cuffed entity, or another entity trying to help uncuff them.
        /// If the uncuffing succeeds, a prototype will be spawned on the floor.
        /// </summary>
        /// <param name="user">The cuffed entity</param>
        /// <param name="isOwner">Is the cuffed entity the owner of the CuffedComponent?</param>
        public async void TryUncuff(IEntity user, bool isOwner)
        {
            var config = _cuffConfigs[_cuffConfigs.Count - 1];

            if (!isOwner && !ActionBlockerSystem.CanInteract(user))
            {
                _notifyManager.PopupMessage(user, user, "You can't do that!");
                return;
            }

            if (!isOwner &&
                !EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(
                    user.Transform.MapPosition,
                    Owner.Transform.MapPosition,
                    _interactRange,
                    ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(user, user, "You are too far away to remove the cuffs.");
                return;
            }

            _notifyManager.PopupMessage(user, user, "You start removing the cuffs.");
            _audioSystem.PlayFromEntity(isOwner ? config.BreakoutSound : config.StartUncuffSound, Owner);

            var uncuffTime = isOwner ? config.BreakoutTime : config.UncuffTime;
            var doAfterEventArgs = new DoAfterEventArgs(user, uncuffTime)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _audioSystem.PlayFromEntity(config.EndUncuffSound, Owner);
                _cuffConfigs.Remove(config);
                CanStillInteract = _hands.Count > CuffedHandCount;
                UpdateStatusEffect();
                Dirty();

                Owner.EntityManager.SpawnEntity(config.PrototypeId, Owner.Transform.GridPosition);

                if (CuffedHandCount == 0)
                {
                    _notifyManager.PopupMessage(user, user, "You successfully remove the cuffs.");

                    if (!isOwner)
                    {
                        _notifyManager.PopupMessage(user, Owner, $"{user.Name} uncuffs your hands.");
                    }

                    _deleteThisFrame = true; // need a better way to do this.
                }
                else
                {
                    if (!isOwner)
                    {
                        _notifyManager.PopupMessage(user, user, $"You successfully remove the cuffs. {CuffedHandCount} of {user.Name}'s hands remain cuffed.");
                        _notifyManager.PopupMessage(user, Owner, $"{user.Name} removes your cuffs. {CuffedHandCount} of your hands remain cuffed.");
                    }
                    else
                    {
                        _notifyManager.PopupMessage(user, user, $"You successfully remove the cuffs. {CuffedHandCount} of your hands remain cuffed.");
                    }
                }
            }
            else
            {
                _notifyManager.PopupMessage(user, user, "You fail to remove the cuffs.");
            }

            return;
        }

        /// <summary>
        /// Allows the uncuffing of a cuffed person. Used by other people and by the component owner to break out of cuffs.
        /// </summary>
        [Verb]
        private sealed class UncuffVerb : Verb<CuffedComponent>
        {
            protected override void GetData(IEntity user, CuffedComponent component, VerbData data)
            {
                if (user != component.Owner && !ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Uncuff");
            }

            protected override void Activate(IEntity user, CuffedComponent component)
            {
                component.TryUncuff(user, isOwner: user.Uid == component.Owner.Uid);
            }
        }
    }
}
