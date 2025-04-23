using System.Linq;
using System.Reflection;
using Robust.Shared.Reflection;

namespace Content.Server._Starlight;

public static class SubscribeExtensions
{
    private static MethodInfo? s_subscribeMethodRefHandler;

    // The load from this reflection is negligible.
    // It would make sense to redo everything like this,
    // but we don’t have multiple subscriptions—needs more thought
    public static void SubscribeAllComponents<TInterface, TEvent>(
        this EntitySystem system,
        IReflectionManager reflection,
        MethodInfo eventHandler,
        Type[]? before = null,
        Type[]? after = null
    )
        where TEvent : notnull
    {
        if (s_subscribeMethodRefHandler == null)
        {
            s_subscribeMethodRefHandler = typeof(EntitySystem)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "SubscribeLocalEvent"
                         && m.IsGenericMethodDefinition
                         && m.GetParameters().Length > 0
                         && m.GetParameters()[0].ParameterType.IsGenericType
                         && m.GetParameters()[0].ParameterType
                              .GetGenericTypeDefinition() == typeof(EntityEventRefHandler<,>)
                )
                .FirstOrDefault();

            if (s_subscribeMethodRefHandler == null)
                throw new InvalidOperationException("SubscribeLocalEvent<TComp,TEvent>(EntityEventRefHandler<TComp,TEvent> not found");
        }

        var compTypes = reflection.GetAllChildren<TInterface>();
        foreach (var compType in compTypes)
        {
            if (!typeof(IComponent).IsAssignableFrom(compType))
                continue;

            var closedSubscribe = s_subscribeMethodRefHandler
                .MakeGenericMethod(compType, typeof(TEvent));

            var closedHandler = eventHandler
                .MakeGenericMethod(compType);

            var delegateType = typeof(EntityEventRefHandler<,>)
                .MakeGenericType(compType, typeof(TEvent));

            var handlerDelegate = Delegate
                .CreateDelegate(delegateType, system, closedHandler);

            closedSubscribe.Invoke(
                system,
                [handlerDelegate, before, after]
            );
        }
    }
}