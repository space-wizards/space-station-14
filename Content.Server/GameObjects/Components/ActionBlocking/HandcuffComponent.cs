#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandcuffComponent))]
    public class HandcuffComponent : SharedHandcuffComponent, IAfterInteract
    {
        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables]
        [DataField("cuffTime")]
        public float CuffTime { get; set; } = 5f;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        [DataField("uncuffTime")]
        public float UncuffTime { get; set; } = 5f;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        [DataField("breakoutTime")]
        public float BreakoutTime { get; set; } = 30f;

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [ViewVariables]
        [DataField("stunBonus")]
        public float StunBonus { get; set; } = 2f;

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [ViewVariables]
        [DataField("breakOnRemove")]
        public bool BreakOnRemove { get; set; }

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        [DataField("cuffedRSI")]
        public string? CuffedRSI { get; set; } = "Objects/Misc/handcuffs.rsi";

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        [DataField("bodyIconState")]
        public string? OverlayIconState { get; set; } = "body-overlay";

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        [DataField("brokenIconState")]
        public string? BrokenState { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        [DataField("brokenName")]
        public string BrokenName { get; set; } = default!;

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        [DataField("brokenDesc")]
        public string BrokenDesc { get; set; } = default!;

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

        [DataField("startCuffSound")]
        public string StartCuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_start.ogg";

        [DataField("endCuffSound")] public string EndCuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_end.ogg";

        [DataField("startBreakoutSound")]
        public string StartBreakoutSound { get; set; } = "/Audio/Items/Handcuffs/cuff_breakout_start.ogg";

        [DataField("startUncuffSound")]
        public string StartUncuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg";

        [DataField("endUncuffSound")]
        public string EndUncuffSound { get; set; } = "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg";
        [DataField("color")]
        public Color Color { get; set; } = Color.White;

        // Non-exposed data fields
        private bool _isBroken = false;

        /// <summary>
        ///     Used to prevent DoAfter getting spammed.
        /// </summary>
        private bool _cuffing;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new HandcuffedComponentState(Broken ? BrokenState : string.Empty);
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (_cuffing) return true;

            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User) || !eventArgs.Target.TryGetComponent<CuffableComponent>(out var cuffed))
            {
                return false;
            }

            if (eventArgs.Target == eventArgs.User)
            {
                eventArgs.User.PopupMessage(Loc.GetString("You can't cuff yourself!"));
                return true;
            }

            if (Broken)
            {
                eventArgs.User.PopupMessage(Loc.GetString("The cuffs are broken!"));
                return true;
            }

            if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var hands))
            {
                eventArgs.User.PopupMessage(Loc.GetString("{0:theName} has no hands!", eventArgs.Target));
                return true;
            }

            if (cuffed.CuffedHandCount == hands.Count)
            {
                eventArgs.User.PopupMessage(Loc.GetString("{0:theName} has no free hands to handcuff!", eventArgs.Target));
                return true;
            }

            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true))
            {
                eventArgs.User.PopupMessage(Loc.GetString("You are too far away to use the cuffs!"));
                return true;
            }

            eventArgs.User.PopupMessage(Loc.GetString("You start cuffing {0:theName}.", eventArgs.Target));
            eventArgs.User.PopupMessage(eventArgs.Target, Loc.GetString("{0:theName} starts cuffing you!", eventArgs.User));

            if (StartCuffSound != null)
                SoundSystem.Play(Filter.Pvs(Owner), StartCuffSound, Owner);

            TryUpdateCuff(eventArgs.User, eventArgs.Target, cuffed);
            return true;
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

            _cuffing = true;

            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterEventArgs);

            _cuffing = false;

            if (result != DoAfterStatus.Cancelled)
            {
                if (cuffs.TryAddNewCuffs(user, Owner))
                {
                    if (EndCuffSound != null)
                        SoundSystem.Play(Filter.Pvs(Owner), EndCuffSound, Owner);

                    user.PopupMessage(Loc.GetString("You successfully cuff {0:theName}.", target));
                    target.PopupMessage(Loc.GetString("You have been cuffed by {0:theName}!", user));
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
