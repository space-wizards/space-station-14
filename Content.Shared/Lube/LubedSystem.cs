using Content.Shared.IdentityManagement;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Lube;

public sealed class LubedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LubedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LubedComponent, ContainerGettingInsertedAttemptEvent>(OnHandPickUp);
        SubscribeLocalEvent<LubedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnInit(EntityUid uid, LubedComponent component, ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(uid);
    }

    private void OnHandPickUp(EntityUid uid, LubedComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        // When predicting dropping a glued item prediction will reinsert the item into the hand when rerolling the state to a previous one.
        // So dropping the item would try to throw it during prediction without this guard statement.
        if (_timing.ApplyingState)
            return;

        args.Cancel();
        var user = args.Container.Owner;

        // Throwing is not predicted yet, so we don't want to predict setting the coordinates either, or it will look weird.
        if (_net.IsServer)
        {
            _transform.SetCoordinates(uid, Transform(user).Coordinates);
            _transform.AttachToGridOrMap(uid);
            _throwing.TryThrow(uid, _random.NextVector2(), baseThrowSpeed: component.SlipStrength);
        }
        _popup.PopupClient(Loc.GetString("lube-slip", ("target", Identity.Entity(uid, EntityManager))), user, user, PopupType.MediumCaution);

        component.SlipsLeft--;
        Dirty(uid, component);
        if (component.SlipsLeft <= 0)
        {
            RemComp<LubedComponent>(uid);
            _nameMod.RefreshNameModifiers(uid);
            return;
        }
    }

    private void OnRefreshNameModifiers(Entity<LubedComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("lubed-name-prefix");
    }
}
