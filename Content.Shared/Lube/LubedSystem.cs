using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Lube;

public sealed partial class LubedSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private NameModifierSystem _nameMod = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LubedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LubedComponent, BeforeGettingEquippedHandEvent>(OnHandPickUp);
        SubscribeLocalEvent<LubedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnInit(Entity<LubedComponent> ent, ref ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(ent.Owner);
    }

    private void OnHandPickUp(Entity<LubedComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        var user = args.User;

        // Throwing is not predicted yet, so we don't want to predict setting the coordinates either, or it will look weird.
        if (_net.IsServer)
        {
            args.Cancelled = true;
            _transform.SetCoordinates(ent, Transform(user).Coordinates);
            _transform.AttachToGridOrMap(ent);
            _throwing.TryThrow(ent, _random.NextVector2(), baseThrowSpeed: ent.Comp.SlipStrength);
            _popup.PopupEntity(Loc.GetString("lube-slip", ("target", Identity.Entity(ent, EntityManager))), user, user, PopupType.MediumCaution);
        }

        ent.Comp.SlipsLeft--;
        Dirty(ent);
        if (ent.Comp.SlipsLeft <= 0)
        {
            RemCompDeferred<LubedComponent>(ent);
            _nameMod.RefreshNameModifiers(ent.Owner);
        }
    }

    private void OnRefreshNameModifiers(Entity<LubedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (ent.Comp.SlipsLeft > 0) // The component is removed deferred, so it might still exist when we refresh.
            args.AddModifier("lubed-name-prefix");
    }
}
