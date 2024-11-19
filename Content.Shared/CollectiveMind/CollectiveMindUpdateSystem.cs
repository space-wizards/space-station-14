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
    
    public void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            if (prototype.RequiredComponent != null && EntityManager.HasComponent(uid, _componentFactory.GetRegistration(prototype.RequiredComponent).Type) && !collective.Minds.Contains(prototype.ID))
                collective.Minds.Add(prototype.ID);
            if (prototype.RequiredTag != null && _tag.HasTag(uid, prototype.RequiredTag) && !collective.Minds.Contains(prototype.ID))
                collective.Minds.Add(prototype.ID);
        }
    }
}