using System.Threading;
using Robust.Shared.Audio;
using Content.Shared.Storage;
using Content.Shared.Mail;

namespace Content.Server.Mail.Components
{
    [RegisterComponent]
    public sealed partial class MailComponent : SharedMailComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("recipient")]
        public string Recipient = "None";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("recipientJob")]
        public string RecipientJob = "None";

        // Why do we not use LockComponent?
        // Because this can't be locked again,
        // and we have special conditions for unlocking,
        // and we don't want to add a verb.
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("isLocked")]
        public bool IsLocked = true;

        /// <summary>
        /// Is this parcel profitable to deliver for the station?
        /// </summary>
        /// <remarks>
        /// The station won't receive any award on delivery if this is false.
        /// This is useful for broken fragile packages and packages that were
        /// not delivered in time.
        /// </remarks>
        [DataField("isProfitable")]
        public bool IsProfitable = true;

        /// <summary>
        /// Is this package considered fragile?
        /// </summary>
        /// <remarks>
        /// This can be set to true in the YAML files for a mail delivery to
        /// always be Fragile, despite its contents.
        /// </remarks>
        [DataField("isFragile")]
        public bool IsFragile = false;

        /// <summary>
        /// Is this package considered priority mail?
        /// </summary>
        /// <remarks>
        /// There will be a timer set for its successful delivery. The
        /// station's bank account will be penalized if it is not delivered on
        /// time.
        ///
        /// This is set to false on successful delivery.
        ///
        /// This can be set to true in the YAML files for a mail delivery to
        /// always be Priority.
        /// </remarks>
        [DataField("isPriority")]
        public bool IsPriority = false;

        /// <summary>
        /// What will be packaged when the mail is spawned.
        /// </summary>
        [DataField("contents")]
        public List<EntitySpawnEntry> Contents = new();

        /// <summary>
        /// The amount that cargo will be awarded for delivering this mail.
        /// </summary>
        [DataField("bounty")]
        public int Bounty = 750;

        /// <summary>
        /// Penalty if the mail is destroyed.
        /// </summary>
        [DataField("penalty")]
        public int Penalty = -250;

        /// <summary>
        /// The sound that's played when the mail's lock is broken.
        /// </summary>
        [DataField("penaltySound")]
        public SoundSpecifier PenaltySound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

        /// <summary>
        /// The sound that's played when the mail's opened.
        /// </summary>
        [DataField("openSound")]
        public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");

        /// <summary>
        /// The sound that's played when the mail's lock has been emagged.
        /// </summary>
        [DataField("emagSound")]
        public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

        /// <summary>
        /// Whether this component is enabled.
        /// Removed when it becomes trash.
        /// </summary>
        public bool IsEnabled = true;

        public CancellationTokenSource? priorityCancelToken;
    }
}
