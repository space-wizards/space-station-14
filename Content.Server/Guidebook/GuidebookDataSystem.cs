using System.Reflection;
using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Guidebook;

public sealed class GuidebookDataSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    private GuidebookData _cachedData = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestGuidebookDataEvent>(OnRequestRules);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        GatherData(ref _cachedData);
    }

    private void OnRequestRules(RequestGuidebookDataEvent ev, EntitySessionEventArgs args)
    {
        var sendEv = new UpdateGuidebookDataEvent(_cachedData);
        RaiseNetworkEvent(sendEv, args.SenderSession);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        RebuildDataCache();

        var ev = new UpdateGuidebookDataEvent(_cachedData);
        RaiseNetworkEvent(ev);
    }

    private void GatherData(ref GuidebookData cache, string? requiredNamespace = null)
    {
        var tagged = new Dictionary<ComponentRegistration, List<FieldInfo>>();
        foreach (var registration in _componentFactory.GetAllRegistrations())
        {
            if (requiredNamespace != null
                && registration.Type.Namespace != null
                && !registration.Type.Namespace.Contains(requiredNamespace))
            {
                continue;
            }

            foreach (var field in registration.Type.GetFields())
            {
                if (field.HasCustomAttribute<GuidebookDataAttribute>())
                {
                    tagged.GetOrNew(registration).Add(field);
                }
            }
        }

        var entityPrototypes = _protoMan.EnumeratePrototypes<EntityPrototype>();
        foreach (var prototype in entityPrototypes)
        {
            foreach (var (type, fields) in tagged)
            {
                if (!prototype.TryGetComponent<IComponent>(type.Name, out var component))
                    continue;

                foreach (var field in fields)
                {
                    //Log.Debug($"{prototype.ID}.{type.Name}.{field.Name}");
                    cache.AddData(prototype.ID, type.Name, field.Name, field.GetValue(component));
                }
            }
        }
    }

    private void RebuildDataCache()
    {
        _cachedData.Clear();
        GatherData(ref _cachedData, "Server");
    }
}
