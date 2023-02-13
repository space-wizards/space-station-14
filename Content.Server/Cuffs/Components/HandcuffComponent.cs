using Content.Server.Administration.Components;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cuffs.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandcuffComponent))]
    public sealed class HandcuffComponent : SharedHandcuffComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [DataField("cuffTime")]
        public float CuffTime { get; set; } = 3.5f;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [DataField("uncuffTime")]
        public float UncuffTime { get; set; } = 3.5f;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [DataField("breakoutTime")]
        public float BreakoutTime { get; set; } = 30f;

        /// <summary>
        ///     If an entity being cuffed is stunned, this amount of time is subtracted from the time it takes to add/remove their cuffs.
        /// </summary>
        [DataField("stunBonus")]
        public float StunBonus { get; set; } = 2f;

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [DataField("breakOnRemove")]
        public bool BreakOnRemove { get; set; }

        /// <summary>
        ///     Will the cuffs break when removed?
        /// </summary>
        [DataField("brokenPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? BrokenPrototype { get; set; }

        /// <summary>
        ///     The path of the RSI file used for the player cuffed overlay.
        /// </summary>
        [DataField("cuffedRSI")]
        public string? CuffedRSI { get; set; } = "Objects/Misc/handcuffs.rsi";

        /// <summary>
        ///     The iconstate used with the RSI file for the player cuffed overlay.
        /// </summary>
        [DataField("bodyIconState")]
        public string? OverlayIconState { get; set; } = "body-overlay";

        [DataField("startCuffSound")]
        public SoundSpecifier StartCuffSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_start.ogg");

        [DataField("endCuffSound")]
        public SoundSpecifier EndCuffSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_end.ogg");

        [DataField("startBreakoutSound")]
        public SoundSpecifier StartBreakoutSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_breakout_start.ogg");

        [DataField("startUncuffSound")]
        public SoundSpecifier StartUncuffSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_start.ogg");

        [DataField("endUncuffSound")]
        public SoundSpecifier EndUncuffSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
        [DataField("color")]
        public Color Color { get; set; } = Color.White;

        /// <summary>
        ///     Used to prevent DoAfter getting spammed.
        /// </summary>
        public bool Cuffing;

        /// <summary>
        /// Update the cuffed state of an entity
        /// </summary>
        public async void TryUpdateCuff(EntityUid user, EntityUid target, CuffableComponent cuffs)
        {
            var cuffTime = CuffTime;

            if (_entities.HasComponent<StunnedComponent>(target))
            {
                cuffTime = MathF.Max(0.1f, cuffTime - StunBonus);
            }

            if (_entities.HasComponent<DisarmProneComponent>(target))
                cuffTime = 0.0f; // cuff them instantly.

            var doAfterEventArgs = new DoAfterEventArgs(user, cuffTime, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            Cuffing = true;

            var result = await EntitySystem.Get<DoAfterSystem>().WaitDoAfter(doAfterEventArgs);

            Cuffing = false;

            // TODO these pop-ups need third-person variants (i.e. {$user} is cuffing {$target}!

            if (result != DoAfterStatus.Cancelled)
            {
                if (cuffs.TryAddNewCuffs(user, Owner))
                {
                    SoundSystem.Play(EndCuffSound.GetSound(), Filter.Pvs(Owner), Owner);
                    if (target == user)
                    {
                        user.PopupMessage(Loc.GetString("handcuff-component-cuff-self-success-message"));
                        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{_entities.ToPrettyString(user):player} has cuffed himself");
                    }
                    else
                    {
                        user.PopupMessage(Loc.GetString("handcuff-component-cuff-other-success-message",("otherName", target)));
                        target.PopupMessage(Loc.GetString("handcuff-component-cuff-by-other-success-message", ("otherName", user)));
                        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{_entities.ToPrettyString(user):player} has cuffed {_entities.ToPrettyString(target):player}");
                    }
                }
            }
            else
            {
                if (target == user)
                {
                    user.PopupMessage(Loc.GetString("handcuff-component-cuff-interrupt-self-message"));
                }
                else
                {
                    user.PopupMessage(Loc.GetString("handcuff-component-cuff-interrupt-message",("targetName", target)));
                    target.PopupMessage(Loc.GetString("handcuff-component-cuff-interrupt-other-message",("otherName", user)));
                }
            }
        }
    }
}
