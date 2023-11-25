using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Audio;

namespace Content.Server.Bodycamera
{
    [RegisterComponent]
    public sealed partial class BodyCameraComponent : Component
    {
        [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled;

        [ViewVariables(VVAccess.ReadWrite), DataField("powerOnSound")]
        public SoundSpecifier? PowerOnSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

        [ViewVariables(VVAccess.ReadWrite), DataField("powerOffSound")]
        public SoundSpecifier? PowerOffSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");
    }
}
