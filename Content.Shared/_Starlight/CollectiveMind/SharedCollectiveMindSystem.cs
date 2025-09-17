using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Content.Shared.GameTicking;

namespace Content.Shared.CollectiveMind;

public abstract partial class SharedCollectiveMindSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    private readonly Dictionary<CollectiveMindPrototype, int> _globalMindIDTracker = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("CollectiveMindUpdateSystem");

        SubscribeLocalEvent<CollectiveMindComponent, ComponentStartup>(OnCollectiveMindInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnCollectiveMindInit(EntityUid uid, CollectiveMindComponent component, ComponentStartup args)
    {
        UpdateCollectiveMind(uid, component);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _globalMindIDTracker.Clear();
    }

    public void ForceCloneFrom(EntityUid sourceuid, EntityUid targetuid)
    {
        if (!TryComp<CollectiveMindComponent>(sourceuid, out var component))
            return;

        if (!TryComp<CollectiveMindComponent>(targetuid, out var targetComponent))
            return;

        targetComponent.Minds.Clear();

        foreach (var mind in component.Minds)
        {
            targetComponent.Minds.Add(mind.Key, mind.Value);
        }

        UpdateCollectiveMind(targetuid, targetComponent); //capture any we missed
    }
    public void UpdateCollectiveMind(EntityUid uid, CollectiveMindComponent collective)
    {
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            var components = StringsToRegs(prototype.RequiredComponents);

            bool meetsRequirements = false;

            foreach (var component in components)
            {
                bool hasComponent = EntityManager.HasComponent(uid, component);

                if (hasComponent)
                {
                    meetsRequirements = true;
                }
            }

            foreach (var tag in prototype.RequiredTags)
            {
                bool hasTag = _tag.HasTag(uid, tag);

                if (hasTag)
                {
                    meetsRequirements = true;
                }
            }

            if (meetsRequirements)
            {
                //check if they dont already have it
                if (collective.Minds.ContainsKey(prototype))
                    continue;

                collective.Minds.TryAdd(prototype, CreateNewCollectiveMindMemberData(prototype));
            }
            else
            {
                collective.Minds.Remove(prototype);
            }
        }
    }

    private List<ComponentRegistration> StringsToRegs(List<string> input)
    {
        var list = new List<ComponentRegistration>();

        if (input == null || input.Count == 0)
            return list;

        foreach (var name in input)
        {
            var availability = _componentFactory.GetComponentAvailability(name);
            if (_componentFactory.TryGetRegistration(name, out var registration)
                && availability == ComponentAvailability.Available)
            {
                list.Add(registration);
            }
            else if (availability == ComponentAvailability.Unknown)
            {
                _sawmill.Error($"StringsToRegs failed: Unknown component name {name} passed to {nameof(SharedCollectiveMindSystem)}.");
            }
        }

        return list;
    }

    private CollectiveMindMemberData CreateNewCollectiveMindMemberData(CollectiveMindPrototype prototype)
    {
        //check if one exists
        if (!_globalMindIDTracker.ContainsKey(prototype))
        {
            _globalMindIDTracker[prototype] = new CollectiveMindMemberData().MindId;
        }

        var data = new CollectiveMindMemberData
        {
            MindId = _globalMindIDTracker[prototype]
        };

        _globalMindIDTracker[prototype]++;

        return data;
    }
}
