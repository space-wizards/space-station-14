using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mining
{
    [RegisterComponent]
    public class PickaxeComponent : Component

    {
        public override string Name => "Pickaxe";
        public string MiningSound;
        public float MiningSpeedMultiplier;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref MiningSound, "miningSound", "/Audio/Items/mining/pickaxe.ogg");
            serializer.DataField(ref MiningSpeedMultiplier, "miningSpeedMultiplier", 1f);
        }
    }
}
