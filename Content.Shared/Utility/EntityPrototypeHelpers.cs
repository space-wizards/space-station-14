#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Utility
{
    public static class EntityPrototypeHelpers
    {
        public static bool HasComponent<T>(string prototype, IPrototypeManager? prototypeManager = null, IComponentFactory? componentFactory = null) where T : IComponent
        {
            return HasComponent(prototype, typeof(T), prototypeManager, componentFactory);
        }

        public static bool HasComponent(string prototype, Type component, IPrototypeManager? prototypeManager = null, IComponentFactory? componentFactory = null)
        {
            prototypeManager ??= IoCManager.Resolve<IPrototypeManager>();
            componentFactory ??= IoCManager.Resolve<IComponentFactory>();

            var registration = componentFactory.GetRegistration(component);

            if (!prototypeManager.TryIndex(prototype, out EntityPrototype proto))
            {
                return proto.Components.ContainsKey(registration.Name);
            }

            return false;
        }
    }
}
