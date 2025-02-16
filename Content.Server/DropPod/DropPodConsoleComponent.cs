using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Server.DropPod
{
    [RegisterComponent]
    public sealed partial class DropPodConsoleComponent : Component
    {
        public bool WarDeclared = false; // This field is needed to prevent players from flying away on a capsule before the declaration of war

        [DataField("announcement")]
        public bool Announcement = true;

        [DataField("text")]
        public string Text = "Attention! A hostile corporation is trying to move an object to your station... The travel time is 12 seconds. The approximate coordinates of the movement are as follows: ";

        [DataField("time")]
        public int Time = 12; // if you change the time, then change it in the text /\

        [DataField("sound")]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/war.ogg");

        [DataField("color")]
        public Color Color = Color.Red;
    }
}
