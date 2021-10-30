using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class PickaxeComponent : Component
    {
        public override string Name => "Pickaxe";

        [DataField("miningSound")]
        public SoundSpecifier MiningSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Mining/pickaxe.ogg");

        [DataField("miningSpeedMultiplier")]
        public float MiningSpeedMultiplier { get; set; } = 1f;
    }
}
