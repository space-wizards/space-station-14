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
using Robust.Shared.Localization;
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
#pragma warning restore 649

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables]
        public float CuffTime { get { return _cuffTime; } set { _cuffTime = value; } }

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        public float UncuffTime { get { return _uncuffTime; } set { _uncuffTime = value; } }

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        public float BreakoutTime { get { return _breakoutTime; } set { _breakoutTime = value; } }

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [ViewVariables]
        public float StunBonus { get { return _stunBonus; } set { _stunBonus = value; } }

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [ViewVariables]
        public bool BreakOnRemove { get { return _breakOnRemove; } set { _breakOnRemove = value; } }

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string CuffedRSI { get { return _cuffedRSI; } set { _cuffedRSI = value; } }

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string OverlayIconState { get { return _overlayIconState; } set { _overlayIconState = value; } }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenState { get { return _brokenState; } set { _brokenState = value; } }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenName { get { return _brokenName; } set { _brokenName = value; } }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenDesc { get { return _brokenDesc; } set { _brokenDesc = value; } }

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

        public string StartCuffSound { get { return _startCuffSound; } set { _startCuffSound = value; } }
        public string EndCuffSound { get { return _endCuffSound; } set { _endCuffSound = value; } }
        public string StartBreakoutSound { get { return _startBreakoutSound; } set { _startBreakoutSound = value; } }
        public string StartUncuffSound { get { return _startUncuffSound; } set { _startUncuffSound = value; } }
        public string EndUncuffSound { get { return _endUncuffSound; } set { _endUncuffSound = value; } }
        public Color Color { get { return _color; } set { _color = value; } }

        // Exposed data fields
        private float _cuffTime;
        private float _breakoutTime;
        private float _uncuffTime;
        private float _stunBonus;
        private string _startUncuffSound;
        private string _endCuffSound;
        private string _startCuffSound;
        private string _endUncuffSound;
        private string _startBreakoutSound;
        private string _cuffedRSI;
        private string _overlayIconState;
        private Color _color;
        private bool _breakOnRemove;
        private string _brokenState;
        private string _brokenName;
        private string _brokenDesc;

        // Non-exposed data fields
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
            serializer.DataField(ref _cuffTime, "cuffTime", 5.0f);
            serializer.DataField(ref _breakoutTime, "breakoutTime", 30.0f);
            serializer.DataField(ref _uncuffTime, "uncuffTime", 5.0f);
            serializer.DataField(ref _stunBonus, "stunBonus", 2.0f);
            serializer.DataField(ref _startCuffSound, "startCuffSound", "/Audio/Items/Handcuffs/cuff_start.ogg");
            serializer.DataField(ref _endCuffSound, "endCuffSound", "/Audio/Items/Handcuffs/cuff_end.ogg");
            serializer.DataField(ref _startUncuffSound, "startUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");
            serializer.DataField(ref _endUncuffSound, "endUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
            serializer.DataField(ref _startBreakoutSound, "startBreakoutSound", "/Audio/Items/Handcuffs/cuff_breakout_start.ogg");
            serializer.DataField(ref _cuffedRSI, "cuffedRSI", "Objects/Misc/handcuffs.rsi");
            serializer.DataField(ref _overlayIconState, "bodyIconState", "body-overlay");
            serializer.DataField(ref _color, "color", Color.White);
            serializer.DataField(ref _breakOnRemove, "breakOnRemove", false);
            serializer.DataField(ref _brokenState, "brokenIconState", string.Empty);
            serializer.DataField(ref _brokenName, "brokenName", string.Empty);
            serializer.DataField(ref _brokenDesc, "brokenDesc", string.Empty);
        }

        public override ComponentState GetComponentState()
        {
            return new HandcuffedComponentState(Broken ? BrokenState : string.Empty);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User) || !eventArgs.Target.TryGetComponent<CuffedComponent>(out var cuffed))
            {
                return;
            }

            if (eventArgs.Target == eventArgs.User)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You can't cuff yourself!"));
                return;
            }

            if (Broken)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("The cuffs are broken!"));
                return;
            }

            if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var hands))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString($"{eventArgs.Target.Name} has no hands!"));
                return;
            }

            if (cuffed.CuffedHandCount == hands.Count)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString($"{eventArgs.Target.Name} has no free hands to handcuff!"));
                return;
            }

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(
                    eventArgs.User.Transform.MapPosition,
                    eventArgs.Target.Transform.MapPosition,
                    _interactRange,
                    ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString("You are too far away to use the cuffs!"));
                return;
            }

            _notifyManager.PopupMessage(eventArgs.User, eventArgs.User, Loc.GetString($"You start cuffing {eventArgs.Target.Name}."));
            _notifyManager.PopupMessage(eventArgs.User, eventArgs.Target, Loc.GetString($"{eventArgs.User.Name} starts cuffing you!"));
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
                _notifyManager.PopupMessage(user, user, Loc.GetString($"You successfully cuff {target.Name}."));
                _notifyManager.PopupMessage(target, target, Loc.GetString($"You have been cuffed by {user.Name}!"));

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
                _notifyManager.PopupMessage(user, user, Loc.GetString($"You fail to cuff {target.Name}!"));
                _notifyManager.PopupMessage(target, target, Loc.GetString($"You interrupt {user.Name} while they are cuffing you!"));
            }
        }
    }
}
