using Content.Shared.Weapons.Ranged;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Radio.Components
{
    [RegisterComponent]
    public sealed partial class RadioChimeComponent : Component
    {
        [DataField]
        public SoundSpecifier? Sound;
    }
}
