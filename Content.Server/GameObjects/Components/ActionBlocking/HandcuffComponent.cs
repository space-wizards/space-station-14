using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Content.Server.GameObjects.Components.GUI;
using Robust.Shared.Serialization;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using Robust.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.Maths;
using System;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class HandcuffComponent : SharedHandcuffComponent, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
#pragma warning restore 649

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables]
        public float CuffTime;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        public float UncuffTime;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        public float BreakoutTime;

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [ViewVariables]
        public float StunBonus;

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [ViewVariables]
        public bool BreakOnRemove = false;

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string CuffedRSI = default;

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string OverlayIconState = default;

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenState = default;

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenName = default;

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenDesc = default;

        [ViewVariables]
        public bool Broken
        {
            get
            {
                return _isBroken;
            }
            set
            {
                if (_isBroken != value)
                {
                    _isBroken = value;

                    Dirty();
                }
            }
        }

        public string StartCuffSound = default;
        public string EndCuffSound = default;
        public string StartBreakoutSound = default;
        public string StartUncuffSound = default;
        public string EndUncuffSound = default;
        public Color Color = default;

        private bool _isBroken = false;
        private float _interactRange;
        private DoAfterSystem _doAfterSystem;
        private AudioSystem _audioSystem;
        
        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
            _doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            _interactRange = SharedInteractionSystem.InteractionRange / 2;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref CuffTime, "cuffTime", 5.0f);
            serializer.DataField(ref BreakoutTime, "breakoutTime", 30.0f);
            serializer.DataField(ref UncuffTime, "uncuffTime", 5.0f);
            serializer.DataField(ref StunBonus, "stunBonus", 2.0f);
            serializer.DataField(ref StartCuffSound, "startCuffSound", "/Audio/Items/Handcuffs/cuff_start.ogg");
            serializer.DataField(ref EndCuffSound, "endCuffSound", "/Audio/Items/Handcuffs/cuff_end.ogg");
            serializer.DataField(ref StartUncuffSound, "startUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");
            serializer.DataField(ref EndUncuffSound, "endUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
            serializer.DataField(ref StartBreakoutSound, "startBreakoutSound", "/Audio/Items/Handcuffs/cuff_breakout_start.ogg");
            serializer.DataField(ref CuffedRSI, "cuffedRSI", "Objects/Misc/handcuffs.rsi");
            serializer.DataField(ref OverlayIconState, "bodyIconState", "body-overlay");
            serializer.DataField(ref Color, "color", Color.White);
            serializer.DataField(ref BreakOnRemove, "breakOnRemove", false);
            serializer.DataField(ref BrokenState, "brokenIconState", string.Empty);
            serializer.DataField(ref BrokenName, "brokenName", string.Empty);
            serializer.DataField(ref BrokenDesc, "brokenDesc", string.Empty);
        }

        public override ComponentState GetComponentState()
        {
            return new HandcuffedComponentState(Broken ? BrokenState : string.Empty);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User))
            {
                return;
            }

            if (eventArgs.Target == eventArgs.User)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, "You can't cuff yourself!");
                return;
            }

            if (Broken)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, "The cuffs are broken!");
                return;
            }

            if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var hands))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"{eventArgs.Target.Name} has no hands!");
                return;
            }

            if (!eventArgs.Target.TryGetComponent<CuffedComponent>(out var cuffed))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"{eventArgs.Target.Name} can't be handcuffed!");
                return;
            }

            if (cuffed.CuffedHandCount == hands.Count)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"{eventArgs.Target.Name} has no free hands to handcuff!");
                return;
            }

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(
                    eventArgs.User.Transform.MapPosition,
                    eventArgs.Target.Transform.MapPosition,
                    _interactRange,
                    ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, "You are too far away to use the cuffs!");
                return;
            }

            _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, $"You start cuffing {eventArgs.Target.Name}.");
            _notifyManager.PopupMessage(eventArgs.User, eventArgs.Target, $"{eventArgs.User.Name} starts cuffing you!");
            _audioSystem.PlayFromEntity(StartCuffSound, Owner);

            TryUpdateCuff(eventArgs.User, eventArgs.Target, cuffed); 
        }

        /// <summary>
        /// Update the cuffed state of an entity
        /// </summary>
        private async void TryUpdateCuff(IEntity user, IEntity target, CuffedComponent cuffs)
        {
            var cuffTime = CuffTime;

            if (target.TryGetComponent<StunnableComponent>(out var stun) && stun.Stunned)
            {
                cuffTime = MathF.Max(0.1f, cuffTime - StunBonus);
            }

            var doAfterEventArgs = new DoAfterEventArgs(user, cuffTime, default, target)
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
                _audioSystem.PlayFromEntity(EndCuffSound, Owner);
                _notifyManager.PopupMessage(user, user, $"You successfully cuff {target.Name}.");
                _notifyManager.PopupMessage(target, target, $"You have been cuffed by {user.Name}!");

                if (user.TryGetComponent<HandsComponent>(out var hands))
                {
                    hands.Drop(Owner);
                    cuffs.AddNewCuffs(Owner);
                }
                else
                {
                    Logger.Warning("Unable to remove handcuffs from player's hands! This should not happen!");
                }
            }
            else
            {
                _notifyManager.PopupMessage(user, user, $"You fail to cuff {target.Name}!");
                _notifyManager.PopupMessage(target, target, $"You interrupt {user.Name} while they are cuffing you!");
            }
        }
    }
}
