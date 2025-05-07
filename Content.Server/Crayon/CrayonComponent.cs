using Content.Server.UserInterface;
using Content.Shared.Crayon;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.Crayon
{
    [RegisterComponent]
    public sealed partial class CrayonComponent : SharedCrayonComponent
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

        /// <summary>
        /// The stored rotation for this crayon's decal, in degrees.
        /// </summary>
        [DataField]
        public float Rotation;

        /// <summary>
        /// The active/unactive status of the preview mode overlay for this crayon.
        /// </summary>
        [DataField]
        public bool PreviewMode;
    }
}
