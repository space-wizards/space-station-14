using Robust.Shared.Audio;

namespace Content.Server.Singularity.PizzaLord.Components
{
    /// <summary>
    /// Makes funny announcement when object with this component is absorbed by Pizza Lord (not the Singulo Lord!)
    /// </summary>
    [RegisterComponent]
    public sealed class AnnounceOnAbsorbComponent : Component
    {
        /// <summary>
        /// Fluent ID for the announcement title
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("title")]
        public string Title = "pizza-lord-itself";
        
        /// <summary>
        /// Fluent ID for the announcement title
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("text")]
        public string Text = "pizza-lord-speech";
        
        /// <summary>
        /// Announcement color
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("color")]
        public Color AnnouncementColor = Color.OrangeRed;
        
        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField("sound")]
        public SoundSpecifier AnnouncementSound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");
    }
}
