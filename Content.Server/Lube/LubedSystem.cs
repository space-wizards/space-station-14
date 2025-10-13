using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Lube;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
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
        SubscribeLocalEvent<LubedComponent, BeforeGettingEquippedHandEvent>(OnHandPickUp);
        SubscribeLocalEvent<LubedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnInit(EntityUid uid, LubedComponent component, ComponentInit args)
    {
        _nameMod.RefreshNameModifiers(uid);
    }

    /// <remarks>
    /// Note to whoever makes this predicted—there is a mispredict here that
    /// would be nice to keep! If this is in shared, the client will predict
    /// this and not run the pickup animation in <see cref="SharedHandsSystem"/>
    /// which would (probably) make this effect look less funny. You will
    /// probably want to either tweak <see cref="BeforeGettingEquippedHandEvent"/>
    /// to be able to cancel but still run the animation or something—we do want
    /// the event to run before the animation for stuff like
    /// <see cref="MultiHandedItemSystem.OnBeforeEquipped"/>.
    /// </remarks>
    private void OnHandPickUp(Entity<LubedComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.SlipsLeft <= 0)
        {
            RemComp<LubedComponent>(ent);
            _nameMod.RefreshNameModifiers(ent.Owner);
            return;
        }

        ent.Comp.SlipsLeft--;
        args.Cancelled = true;

        _transform.SetCoordinates(ent, Transform(args.User).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), ent.Comp.SlipStrength);
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
