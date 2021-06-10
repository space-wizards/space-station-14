using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Mining.Components
{
    [RegisterComponent]
    public class PickaxeComponent : Component

    {
        public override string Name => "Pickaxe";
        [DataField("miningSound")]
        public string MiningSound = "/Audio/Items/Mining/pickaxe.ogg";
        [DataField("miningSpeedMultiplier")]
        public float MiningSpeedMultiplier = 1f;
    }
}
