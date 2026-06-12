using System.Linq;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.Organs;

public sealed partial class WoundableOrganSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableOrganComponent, ExaminedEvent>(OnBeingExamined);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<WoundableOrganWeightsEvent>>(OnGetWeights);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetWoundsWithSpaceEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetPainEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<HealWoundsEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<GetBleedLevelEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<ClampWoundsEvent>>(UnwrapRelay);
        SubscribeLocalEvent<WoundableOrganComponent, BodyRelayedEvent<BeforeEquippingHandEvent>>(UnwrapRelay);
    }

    private void OnGetWeights(Entity<WoundableOrganComponent> ent, ref BodyRelayedEvent<WoundableOrganWeightsEvent> args)
    {
        args.Args.Weights[ent] = ent.Comp.Weight;
    }

    public Dictionary<Entity<WoundableOrganComponent>, float> GetWoundableOrgans(EntityUid body)
    {
        var organs = new WoundableOrganWeightsEvent(new());
        RaiseLocalEvent(body, ref organs);
        return organs.Weights;
    }

    private void UnwrapRelay<TEvent>(Entity<WoundableOrganComponent> ent, ref BodyRelayedEvent<TEvent> args) where TEvent : struct
    {
        var evt = args.Args;
        RaiseLocalEvent(ent, ref evt);
        args.Args = evt;
    }

    private static readonly LocId WoundCountModifier = "wound-count-modifier";
    private static readonly LocId WoundCountModifierExterior = "wound-count-modifier-exterior";
    private static readonly LocId WoundCountNone = "wound-count-none";

    private void OnBeingExamined(Entity<WoundableOrganComponent> organ, ref ExaminedEvent args)
    {
        if (!_statusEffects.TryEffectsWithComp<WoundDescriptionComponent>(organ, out var wounds))
            wounds = new();

        var counts = new Dictionary<(LocId, LocId?, LocId?), int>();

        foreach (var describable in wounds)
        {
            var wound = Comp<WoundComponent>(describable);
            var damage = wound.Damage.GetTotal();

            if (describable.Comp1.Descriptions.HighestMatch(damage) is not { } message)
                continue;

            var text = message;
            LocId? bleedingMessage = null;
            LocId? tendedMessage = null;

            if (TryComp<BleedingWoundComponent>(describable, out var bleeding) && _woundable.BleedLevel((describable.Owner, bleeding)) > 0f)
                bleedingMessage = describable.Comp1.BleedingModifier;

            if (TryComp<TendableWoundComponent>(describable, out var tendable) && tendable.Tended)
                tendedMessage = describable.Comp1.TendedModifier;

            var triple = (text, bleedingMessage, tendedMessage);

            if (counts.TryGetValue(triple, out var count))
                counts[triple] = count + 1;
            else
                counts[triple] = 1;
        }

        var organComp = Comp<OrganComponent>(organ);
        foreach (var (triple, count) in counts.OrderBy(it => it.Key.Item1))
        {
            var text = Loc.GetString(triple.Item1, ("count", count));
            if (triple.Item2 is { } bleedingMessage)
                text = Loc.GetString(bleedingMessage, ("wound", text));
            if (triple.Item3 is { } tendedMessage)
                text = Loc.GetString(tendedMessage, ("wound", text));

            if (organComp.Body is { } body)
                args.PushMarkup(Loc.GetString(WoundCountModifier, ("wound", text), ("count", count), ("target", Identity.Entity(body, EntityManager)), ("organ", organ)));
            else
                args.PushMarkup(Loc.GetString(WoundCountModifierExterior, ("wound", text), ("count", count), ("organ", organ)));
        }

        if (counts.Count == 0 && organComp.Body is { } organBody)
        {
            args.PushMarkup(Loc.GetString(WoundCountNone, ("target", Identity.Entity(organBody, EntityManager)), ("organ", organ)));
        }
    }
}
