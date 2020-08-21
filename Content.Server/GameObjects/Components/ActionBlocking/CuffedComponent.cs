using System;
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
        }

#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
#pragma warning restore 649

        /// <summary>
        /// In cases where an entity has >2 hands, we use this variable to track how many are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => _cuffConfigs.Count * 2;

        /// <summary>
        /// Every set of hands tracks its own handcuff config. This is used in cases where an entity with >2 hands is cuffed using different types of cuffs.
        /// </summary>
        [ViewVariables]
        private List<CuffConfig> _cuffConfigs;

        private bool _dirtyThisFrame = false;
        private float _interactRange;
        private DoAfterSystem _doAfterSystem;
        private HandsComponent _hands;

        public override void Initialize()
        {
            base.Initialize();

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
            return new CuffedComponentState(CuffedHandCount, CanStillInteract);
        }

        /// <summary>
        /// Add a set of cuffs to an existing CuffedComponent.
        /// </summary>
        /// <param name="prototype"></param>
        public void AddNewCuffs(CuffConfig config)
        {
            _cuffConfigs.Add(config);
        }

        /// <summary>
        /// Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        public void UpdateHandCount() // TODO: WE NEED TO CALL THIS FROM AN UPDATE OR SOMETHING TO KEEP THE CUFFS SYNCED WITH THE HANDS
        {
            _dirtyThisFrame = false;

            while (CuffedHandCount > _hands.Count && CuffedHandCount > 0)
            {
                _dirtyThisFrame = true;

                var config = _cuffConfigs[_cuffConfigs.Count - 1];
                _cuffConfigs.Remove(config);
                
                Owner.EntityManager.SpawnEntity(config.PrototypeId, Owner.Transform.GridPosition);
            }

            if (CuffedHandCount == 0)
            {
                Owner.RemoveComponent<CuffedComponent>();
            }
            else if (_dirtyThisFrame)
            {
                CanStillInteract = _hands.Count > CuffedHandCount;
                Dirty();
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

            var uncuffTime = isOwner ? config.BreakoutTime : config.UncuffTime;
            var doAfterEventArgs = new DoAfterEventArgs(user, uncuffTime)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _cuffConfigs.Remove(config);
                CanStillInteract = _hands.Count > CuffedHandCount;
                Dirty();

                Owner.EntityManager.SpawnEntity(config.PrototypeId, Owner.Transform.GridPosition);

                if (CuffedHandCount == 0)
                {
                    _notifyManager.PopupMessage(user, user, "You successfully remove the cuffs.");

                    if (!isOwner)
                    {
                        _notifyManager.PopupMessage(user, Owner, $"{user.Name} removes your cuffs.");
                    }

                    Owner.RemoveComponent<CuffedComponent>();
                }
                else
                {
                    _notifyManager.PopupMessage(user, user, $"You successfully remove the cuffs. {CuffedHandCount} of your hands remain cuffed.");

                    if (!isOwner)
                    {
                        _notifyManager.PopupMessage(user, Owner, $"{user.Name} removes your cuffs. {CuffedHandCount} of your hands remain cuffed.");
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
