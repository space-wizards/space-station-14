using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class HandcuffComponent : SharedHandcuffComponent, IAfterInteract
    {
        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables]
        [YamlField("cuffTime")]
        public float CuffTime { get; set; } = 5f;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        [YamlField("uncuffTime")]
        public float UncuffTime { get; set; } = 5f;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        [YamlField("breakoutTime")]
        public float BreakoutTime { get; set; } = 30f;

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [ViewVariables]
        [YamlField("stunBonus")]
        public float StunBonus { get; set; } = 2f;

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [ViewVariables]
        [YamlField("breakOnRemove")]
        public bool BreakOnRemove { get; set; }

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        [YamlField("cuffedRSI")]
        public string CuffedRSI { get; set; } = "Objects/Misc/handcuffs.rsi";

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        [YamlField("bodyIconState")]
        public string OverlayIconState { get; set; } = "body-overlay";

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        [YamlField("brokenIconState")]
        public string BrokenState { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        [YamlField("brokenName")]
        public string BrokenName { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        [YamlField("brokenDesc")]
        public string BrokenDesc { get; set; }

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

        [YamlField("startCuffSound")]
        public string StartCuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_start.ogg";

        [YamlField("endCuffSound")] public string EndCuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_end.ogg";

        [YamlField("startBreakoutSound")]
        public string StartBreakoutSound { get; set; } = "/Audio/Items/Handcuffs/cuff_breakout_start.ogg";

        [YamlField("startUncuffSound")]
        public string StartUncuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg";

        [YamlField("endUncuffSound")]
        public string EndUncuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg";
        [YamlField("color")]
        public Color Color { get; set; } = Color.White;

        // Non-exposed data fields
        private bool _isBroken = false;
        private float _interactRange;
        private AudioSystem _audioSystem;

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = EntitySystem.Get<AudioSystem>();
            _interactRange = SharedInteractionSystem.InteractionRange / 2;
        }

        public override ComponentState GetComponentState()
        {
            return new HandcuffedComponentState(Broken ? BrokenState : string.Empty);
        }

        async Task IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User) || !eventArgs.Target.TryGetComponent<CuffableComponent>(out var cuffed))
            {
                return;
            }

            if (eventArgs.Target == eventArgs.User)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You can't cuff yourself!"));
                return;
            }

            if (Broken)
            {
                eventArgs.User.PopupMessage(Loc.GetString("The cuffs are broken!"));
                return;
            }

            if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var hands))
            {
                eventArgs.User.PopupMessage(Loc.GetString("{0:theName} has no hands!", eventArgs.Target));
                return;
            }

            if (cuffed.CuffedHandCount == hands.Count)
            {
                eventArgs.User.PopupMessage(Loc.GetString("{0:theName} has no free hands to handcuff!", eventArgs.Target));
                return;
            }

            if (!eventArgs.InRangeUnobstructed(_interactRange, ignoreInsideBlocker: true))
            {
                eventArgs.User.PopupMessage(Loc.GetString("You are too far away to use the cuffs!"));
                return;
            }

            eventArgs.User.PopupMessage(Loc.GetString("You start cuffing {0:theName}.", eventArgs.Target));
            eventArgs.User.PopupMessage(eventArgs.Target, Loc.GetString("{0:theName} starts cuffing you!", eventArgs.User));
            _audioSystem.PlayFromEntity(StartCuffSound, Owner);

            TryUpdateCuff(eventArgs.User, eventArgs.Target, cuffed);
        }

        /// <summary>
        /// Update the cuffed state of an entity
        /// </summary>
        private async void TryUpdateCuff(IEntity user, IEntity target, CuffableComponent cuffs)
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

            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _audioSystem.PlayFromEntity(EndCuffSound, Owner);
                user.PopupMessage(Loc.GetString("You successfully cuff {0:theName}.", target));
                target.PopupMessage(Loc.GetString("You have been cuffed by {0:theName}!", user));

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
                user.PopupMessage(Loc.GetString("You were interrupted while cuffing {0:theName}!", target));
                target.PopupMessage(Loc.GetString("You interrupt {0:theName} while they are cuffing you!", user));
            }
        }
    }
}
