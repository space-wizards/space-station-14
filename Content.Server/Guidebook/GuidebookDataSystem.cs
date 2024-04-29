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
        var fieldCount = 0;
        var tagged = new Dictionary<ComponentRegistration, List<MemberInfo>>();
        foreach (var registration in _componentFactory.GetAllRegistrations())
        {
            if (requiredNamespace != null
                && registration.Type.Namespace != null
                && !registration.Type.Namespace.Contains(requiredNamespace))
            {
                continue;
            }

            foreach (var member in registration.Type.GetMembers())
            {
                if (member.HasCustomAttribute<GuidebookDataAttribute>())
                {
                    tagged.GetOrNew(registration).Add(member);
                    fieldCount++;
                }
            }
        }

        var prototypeCount = 0;
        var entityPrototypes = _protoMan.EnumeratePrototypes<EntityPrototype>();
        foreach (var prototype in entityPrototypes)
        {
            foreach (var (type, members) in tagged)
            {
                if (!prototype.TryGetComponent<IComponent>(type.Name, out var component))
                    continue;

                prototypeCount++;

                foreach (var member in members)
                {
                    var value = member switch
                    {
                        FieldInfo field => field.GetValue(component),
                        PropertyInfo property => property.GetValue(component),
                        _ => throw new NotImplementedException("Unsupported member type")
                    };
                    cache.AddData(prototype.ID, type.Name, member.Name, value);
                }
            }
        }

        Log.Debug($"Collected {cache.Count} Guidebook Protodata value(s) - {prototypeCount} matched prototype(s), {tagged.Count} component(s), {fieldCount} field(s)");
    }

    private void RebuildDataCache()
    {
        _cachedData.Clear();
        GatherData(ref _cachedData, "Server");
    }
}
