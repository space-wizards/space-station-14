using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Flash
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedFlashSystem))]
    public sealed partial class FlashComponent : Component
    {
        [DataField("duration")]
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public int FlashDuration { get; set; } = 5000;

        [DataField("range")]
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Range { get; set; } = 7f;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        [DataField("aoeFlashDuration")]
        public int AoeFlashDuration { get; set; } = 2000;

        [DataField("slowTo")]
        [AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowTo { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        [DataField("sound")]
        public SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

        public bool Flashing;
    }
}
