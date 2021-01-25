using System;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Cargo.RequestSpecifiers
{
    public class ComponentRequestSpecifier : RequestSpecifier
    {
        public Type ComponentType { get; private set; }

        public override int EntityToUnits(IEntity entity)
        {
            return entity.HasComponent(ComponentType) ? 1 : 0;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            string componentType = null;
            serializer.DataField(ref componentType, "type", "");
            if (string.IsNullOrEmpty(componentType))
            {
                Logger.Error($"Failed to serialize componentType for {nameof(ComponentRequestSpecifier)}");
                return;
            }

            var registration = IoCManager.Resolve<IComponentFactory>().GetRegistration(componentType);
            ComponentType = registration.Type;
        }
    }
}
