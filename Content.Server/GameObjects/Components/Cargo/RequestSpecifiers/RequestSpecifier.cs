using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Cargo.RequestSpecifiers
{
    public abstract class RequestSpecifier : IExposeData
    {
        public int Amount { get; protected set; }

        public abstract int EntityToUnits(IEntity entity);

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }
    }
}
