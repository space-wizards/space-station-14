using Content.Server.UserInterface;
using Content.Shared.Crayon;
using Content.Shared.Sound;
using Robust.Server.GameObjects;

namespace Content.Server.Crayon
{
    [RegisterComponent]
    public sealed class CrayonComponent : SharedCrayonComponent
    {
        [DataField("useSound")] public SoundSpecifier? UseSound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("selectableColor")]
        public bool SelectableColor { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Charges { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("capacity")]
        public int Capacity { get; set; } = 30;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("deleteEmpty")]
        public bool DeleteEmpty = true;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(CrayonUiKey.Key);
    }
}
