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
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
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
        public float CuffTime { get; set; }

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables]
        public float UncuffTime { get; set; }

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables]
        public float BreakoutTime { get; set; }

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [ViewVariables]
        public float StunBonus { get; set; }

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [ViewVariables]
        public bool BreakOnRemove { get; set; }

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string? CuffedRSI { get; set; }

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [ViewVariables]
        public string? OverlayIconState { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string? BrokenState { get; set; }

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
        public string BrokenName { get; set; } = default!;

        /// <summary>
        ///     The iconstate used for broken handcuffs
        /// </summary>
        [ViewVariables]
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

        public string? StartCuffSound { get; set; }
        public string? EndCuffSound { get; set; }
        public string? StartBreakoutSound { get; set; }
        public string? StartUncuffSound { get; set; }
        public string? EndUncuffSound { get; set; }
        public Color Color { get; set; }

        // Non-exposed data fields
        private bool _isBroken = false;

        /// <summary>
        ///     Used to prevent DoAfter getting spammed.
        /// </summary>
        private bool _cuffing;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.CuffTime, "cuffTime", 5.0f);
            serializer.DataField(this, x => x.BreakoutTime, "breakoutTime", 30.0f);
            serializer.DataField(this, x => x.UncuffTime, "uncuffTime", 5.0f);
            serializer.DataField(this, x => x.StunBonus, "stunBonus", 2.0f);
            serializer.DataField(this, x => x.StartCuffSound, "startCuffSound", "/Audio/Items/Handcuffs/cuff_start.ogg");
            serializer.DataField(this, x => x.EndCuffSound, "endCuffSound", "/Audio/Items/Handcuffs/cuff_end.ogg");
            serializer.DataField(this, x => x.StartUncuffSound, "startUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");
            serializer.DataField(this, x => x.EndUncuffSound, "endUncuffSound", "/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
            serializer.DataField(this, x => x.StartBreakoutSound, "startBreakoutSound", "/Audio/Items/Handcuffs/cuff_breakout_start.ogg");
            serializer.DataField(this, x => x.CuffedRSI, "cuffedRSI", "Objects/Misc/handcuffs.rsi");
            serializer.DataField(this, x => x.OverlayIconState, "bodyIconState", "body-overlay");
            serializer.DataField(this, x => x.Color, "color", Color.White);
            serializer.DataField(this, x => x.BreakOnRemove, "breakOnRemove", false);
            serializer.DataField(this, x => x.BrokenState, "brokenIconState", string.Empty);
            serializer.DataField(this, x => x.BrokenName, "brokenName", string.Empty);
            serializer.DataField(this, x => x.BrokenDesc, "brokenDesc", string.Empty);
        }

        public override ComponentState GetComponentState()
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
                EntitySystem.Get<AudioSystem>().PlayFromEntity(StartCuffSound, Owner);

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
                        EntitySystem.Get<AudioSystem>().PlayFromEntity(EndCuffSound, Owner);

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
