using Content.Server.Disposal.Unit.Components;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    public sealed partial class DisposalTaggerComponent : DisposalTransitComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("tag")]
        public string Tag = "";

        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    }
}
