using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Prototypes
{
    [UsedImplicitly]
    [Obsolete("More efficient methods exist in EntityPrototyp.HasComp and EntitySystem's proxy methods")]
    public static class EntityPrototypeHelpers
    {
        [Obsolete("More efficient methods exist in EntityPrototype.HasComp and EntitySystem's proxy methods")]
        public static bool HasComponent<T>(this EntityPrototype prototype, IComponentFactory? componentFactory = null) where T : IComponent
        {
            return prototype.HasComponent(typeof(T), componentFactory);
        }

        [Obsolete("More efficient methods exist in EntityPrototype.HasComp and EntitySystem's proxy methods")]
        public static bool HasComponent(this EntityPrototype prototype, Type component, IComponentFactory? componentFactory = null)
        {
            componentFactory ??= IoCManager.Resolve<IComponentFactory>();

            var registration = componentFactory.GetRegistration(component);

            return prototype.Components.ContainsKey(registration.Name);
        }

        [Obsolete("More efficient methods exist in EntityPrototype.HasComp and EntitySystem's proxy methods")]
        public static bool HasComponent<T>(string prototype, IPrototypeManager? prototypeManager = null, IComponentFactory? componentFactory = null) where T : IComponent
        {
            return HasComponent(prototype, typeof(T), prototypeManager, componentFactory);
        }

        [Obsolete("More efficient methods exist in EntityPrototype.HasComp and EntitySystem's proxy methods")]
        public static bool HasComponent(string prototype, Type component, IPrototypeManager? prototypeManager = null, IComponentFactory? componentFactory = null)
        {
            prototypeManager ??= IoCManager.Resolve<IPrototypeManager>();

            return prototypeManager.TryIndex(prototype, out EntityPrototype? proto) && proto.HasComponent(component, componentFactory);
        }
    }
}
