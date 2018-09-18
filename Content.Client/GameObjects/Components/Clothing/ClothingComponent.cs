using Content.Shared.GameObjects.Components.Inventory;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Renderable;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Clothing
{
    public class ClothingComponent : Component
    {
        public override string Name => "Clothing";

        [ViewVariables] private ResourcePath _rsiPath;
        private string _prefix;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Prefix
        {
            get => _prefix;
            // TODO: Setting this should update the mob if equipped.
            set => _prefix = value;
        }

        public (RSI rsi, RSI.StateId stateId)? GetEquippedStateInfo(EquipmentSlotDefines.SlotFlags slot)
        {
            if (_rsiPath == null)
            {
                return null;
            }

            var rsi = GetRSI();
            var stateId = $"{_prefix}-{slot}";
            if (rsi.TryGetState(stateId, out _))
            {
                return (rsi, stateId);
            }

            return null;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref _rsiPath, "sprite", null);
            serializer.DataFieldCached(ref _prefix, "prefix", "equipped");
        }

        private RSI GetRSI()
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();
            return resourceCache.GetResource<RSIResource>(SharedSpriteComponent.TextureRoot / _rsiPath).RSI;
        }
    }
}
