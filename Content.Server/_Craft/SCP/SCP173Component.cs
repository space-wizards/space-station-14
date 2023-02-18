using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Server.Abilities.SCP.ConcreteSlab
{
    [RegisterComponent]
    public sealed class SCP173Component : Component
    {
        [DataField("enabled")]
        public bool Enabled = true;

        [DataField("eyeSightRange")]
        public float EyeSightRange = 8;

        [DataField("lookedAt")]
        public bool LookedAt = false;

        [DataField("spooksSoundCollection", required: true)]
        public SoundSpecifier SpooksSound = default!;

        [DataField("scaresSoundCollection", required: true)]
        public SoundSpecifier ScaresSound = default!;

        [DataField("killSoundCollection", required: true)]
        public SoundSpecifier KillSound = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("lookers")]
        public List<EntityUid> Lookers = new();

        [DataField("shartAction")]
        public InstantAction ShartAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(32),
            Icon = new SpriteSpecifier.Texture(new("Interface/Actions/malfunction.png")),
            DisplayName = "scp-173-shart",
            Description = "scp-173-shart-desc",
            Priority = -1,
            Event = new ShartActionEvent(),
        };
        [DataField("blindAction")]
        public InstantAction BlindAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(90),
            Icon = new SpriteSpecifier.Texture(new("Interface/Actions/malfunction.png")),
            DisplayName = "scp-173-blind",
            Description = "scp-173-blind-desc",
            Priority = -1,
            Event = new BlindActionEvent(),
        };
    }
}
