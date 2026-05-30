using Content.Shared.Body;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed partial class HandIncapacitationStatusEffectSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, BeforeEquippingHandEvent>(_body.RelayEvent);
        SubscribeLocalEvent<HandIncapacitationStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<HandIncapacitationStatusEffectComponent, StatusEffectRelayedEvent<BeforeEquippingHandEvent>>(OnBeforeEquippingHand);
    }

    private void OnStatusEffectApplied(Entity<HandIncapacitationStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var status) || status.AppliedTo is not { } organ)
            return;

        if (!TryComp<HandOrganComponent>(organ, out var handOrgan))
            return;

        if (!TryComp<OrganComponent>(organ, out var organComp) || organComp.Body is not { } body)
            return;

        _hands.TryDrop((body, null), handOrgan.HandID);
    }

    private void OnBeforeEquippingHand(Entity<HandIncapacitationStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BeforeEquippingHandEvent> args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var status) || status.AppliedTo is not { } organ)
            return;

        if (!TryComp<HandOrganComponent>(organ, out var handOrgan))
            return;

        if (args.Args.HandId != handOrgan.HandID)
            return;

        args.Args = args.Args with { Cancelled = true };
    }
}
