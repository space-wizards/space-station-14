using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Cargo.RequestSpecifiers
{
    public class PrototypeRequestSpecifier : RequestSpecifier
    {
        public string PrototypeID { get; private set; }

        public override int EntityToUnits(IEntity entity)
        {
            return entity.Prototype?.ID == PrototypeID ? 1 : 0;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.PrototypeID, "prototype", null);
            if (string.IsNullOrEmpty(PrototypeID))
            {
                Logger.Error($"Failed to serialize prototypeID for {nameof(PrototypeRequestSpecifier)}");
            }
        }
    }
}
