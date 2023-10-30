using Content.Server.Ganimed;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Ganimed.Components
{
    [RegisterComponent]
    [Access(typeof(BookTerminalSystem))]
    public sealed partial class BookTerminalComponent : Component
    {

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");
    }
}
