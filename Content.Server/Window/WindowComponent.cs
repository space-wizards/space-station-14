using System;
using Content.Shared.Sound;
using Content.Shared.Window;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Window
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent
    {
        [DataField("knockDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan KnockDelay = TimeSpan.FromSeconds(0.5);

        [DataField("knockSound")]
        public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/Effects/glass_knock.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan LastKnockTime;
    }
}
