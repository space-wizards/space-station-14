using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [Access(typeof(DisposalTubeSystem))]
    public sealed partial class DisposalRouterComponent : DisposalJunctionComponent
    {
        [DataField("tags")]
        public HashSet<string> Tags = new();

        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    }
}
