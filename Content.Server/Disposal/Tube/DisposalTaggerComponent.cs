// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Audio;

namespace Content.Server.Disposal.Tube
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
