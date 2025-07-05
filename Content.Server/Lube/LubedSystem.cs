using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Lube;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Lube;

public sealed class LubedSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LubedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LubedComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUp);
        SubscribeLocalEvent<LubedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnInit(EntityUid uid, LubedComponent component, ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(uid);
    }

    private void OnGettingPickedUp(Entity<LubedComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!args.PhysicalAttempt)
            return;

        if (ent.Comp.SlipsLeft <= 0)
        {
            RemComp<LubedComponent>(ent);
            _nameMod.RefreshNameModifiers(ent.Owner);
            return;
        }

        args.Cancel();
        ent.Comp.SlipsLeft--;
        _transform.SetCoordinates(ent, Transform(args.User).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), baseThrowSpeed: ent.Comp.SlipStrength);
        _popup.PopupEntity(Loc.GetString("lube-slip", ("target", Identity.Entity(ent, EntityManager))),
            args.User,
            args.User,
            PopupType.MediumCaution);
    }

    private void OnRefreshNameModifiers(Entity<LubedComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("lubed-name-prefix");
    }
}
