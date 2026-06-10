using Content.Shared.Body;

namespace Content.Client._Offbrand.BodyVisuals;

public sealed partial class BodyAppearanceRelaySystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyAppearanceRelayComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BodyComponent, BodyAppearanceRelayTargetAddedEvent>(_body.RelayEvent);
        SubscribeLocalEvent<BodyComponent, BodyAppearanceRelayTargetRemovedEvent>(_body.RelayEvent);
    }

    /// <summary>
    /// Adds a target entity to be relayed to for this body
    /// </summary>
    /// <param name="body">The body to relay from</param>
    /// <param name="target">The target to relay to</param>
    public void AddTarget(EntityUid body, EntityUid target)
    {
        var relay = EnsureComp<BodyAppearanceRelayComponent>(body);
        if (!relay.Targets.Add(target))
            return;

        var ev = new BodyAppearanceRelayTargetAddedEvent(target);
        RaiseLocalEvent(body, ref ev);
    }

    /// <summary>
    /// Removes a target entity from being relayed for this body
    /// </summary>
    /// <param name="body">The body to stop relaying from</param>
    /// <param name="target">The target to stop relaying to</param>
    public void RemoveTarget(Entity<BodyAppearanceRelayComponent?> body, EntityUid target)
    {
        if (!Resolve(body, ref body.Comp))
            return;

        if (!body.Comp.Targets.Remove(target))
            return;

        var ev = new BodyAppearanceRelayTargetRemovedEvent(target);
        RaiseLocalEvent(body, ref ev);
    }

    /// <returns>A list of entities to apply sprites to for this body, including the body</returns>
    public IEnumerable<EntityUid> GetTargets(EntityUid body)
    {
        yield return body;

        if (!TryComp<BodyAppearanceRelayComponent>(body, out var relay))
            yield break;

        foreach (var target in relay.Targets)
        {
            if (Exists(target))
                yield return target;
        }
    }

    private void OnShutdown(Entity<BodyAppearanceRelayComponent> ent, ref ComponentShutdown args)
    {
        foreach (var target in ent.Comp.Targets)
        {
            var ev = new BodyAppearanceRelayTargetRemovedEvent(target);
            RaiseLocalEvent(ent, ref ev);
        }

        ent.Comp.Targets.Clear();
    }
}
