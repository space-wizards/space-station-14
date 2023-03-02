using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Server.NukeOps
{
    [RegisterComponent]
    public sealed class WarDeclaratorComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("message")]
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Default message to send
        /// </summary>
        [DataField("defaultMessage")]
        public string DefaultMessage { get; set; } = string.Empty;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxMessageLength")]
        public int MaxMessageLength { get; set; } = 255;
        
        /// <summary>
        /// War declarement text color
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("color")]
        public Color DeclarementColor = Color.Red;

        /// <summary>
        /// War declarement sound file path
        /// </summary>
        [DataField("sound")]
        public SoundSpecifier DeclarementSound = new SoundPathSpecifier("/Audio/Announcements/war.ogg");
        
        /// <summary>
        /// Fluent ID for the declarement title
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("title")]
        public string DeclarementDisplayName = "comms-console-announcement-title-nukie";
    }
}
