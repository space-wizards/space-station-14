using Content.Shared.CollectiveMind;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Shared.CollectiveMind;

public sealed class CollectiveMindUpdateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    
    private static int _currentId = 0;

    public void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            foreach (var component in prototype.RequiredComponents)
                if (EntityManager.HasComponent(uid, _componentFactory.GetRegistration(component).Type) && !collective.Minds.Contains(prototype.ID))
                    collective.Minds.Add(prototype.ID);
            foreach (var tag in prototype.RequiredTags)
                if (_tag.HasTag(uid, tag) && !collective.Minds.Contains(prototype.ID))
                    collective.Minds.Add(prototype.ID);
        }
        
        if (collective.UniqueId == null)
            collective.UniqueId = ++_currentId;
    }
}