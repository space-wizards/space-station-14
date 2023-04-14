using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    private const string WoundContainerId = "WoundSystemWounds";

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private readonly Dictionary<string, WoundTable> _cachedWounds = new();

    public override void Initialize()
    {
        CacheWoundData();
        _prototypeManager.PrototypesReloaded += _ => CacheWoundData();

        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableInit);

        SubscribeLocalEvent<BodyComponent, DamageChangedEvent>(OnBodyDamaged);
        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnBodyRejuvenate);
    }

    private void OnWoundableInit(EntityUid uid, WoundableComponent component, ComponentInit args)
    {
        if (component.Health <= 0)
            component.Health = component.MaxIntegrity;
        if (component.Integrity <= 0)
            component.Integrity = component.MaxIntegrity;
    }

    private void OnBodyRejuvenate(EntityUid uid, BodyComponent component, RejuvenateEvent args)
    {
        HealAllWounds(uid, component);
        //TODO: reset bodypart health/structure values
    }

    private void CacheWoundData()
    {
        _cachedWounds.Clear();

        foreach (var traumaType in _prototypeManager.EnumeratePrototypes<TraumaPrototype>())
        {
            _cachedWounds.Add(traumaType.ID, new WoundTable(traumaType));
        }
    }

    public override void Update(float frameTime)
    {
        UpdateHealing(frameTime);
    }

    private void OnBodyDamaged(EntityUid uid, BodyComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;
        //TODO targeting
        var parts = _body.GetBodyChildren(uid, component).ToList();
        var part = _random.Pick(parts);
        TryApplyTrauma(args.Origin, part.Id, args.DamageDelta);
    }

    private readonly struct WoundTable
    {
        private readonly SortedDictionary<FixedPoint2, string> _wounds;

        public WoundTable(TraumaPrototype trauma)
        {
            _wounds = trauma.Wounds;
        }

        public IEnumerable<KeyValuePair<FixedPoint2, string>> HighestToLowest()
        {
            return _wounds.Reverse();
        }
    }
}
