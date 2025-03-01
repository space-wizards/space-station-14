using Robust.Shared.Audio;

namespace Content.Shared.Starlight.Medical.Items.Components;

[RegisterComponent]
public sealed partial class PatchComponent : Component
{
    [DataField]
    public string SolutionContainer = "patch";
    
        /// <summary>
        /// How long it takes to apply patch.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 3f;
        
        /// <summary>
        ///     Sound played on apply begin
        /// </summary>
        [DataField("healingBeginSound")]
        public SoundSpecifier? ApplyBeginSound = null;

        /// <summary>
        ///     Sound played on apply end
        /// </summary>
        [DataField("healingEndSound")]
        public SoundSpecifier? ApplyEndSound = null;
}