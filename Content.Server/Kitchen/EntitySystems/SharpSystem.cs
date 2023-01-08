using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Kitchen.Components;
using Content.Server.MobState;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Shared.Destructible;
using Robust.Server.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Kitchen.EntitySystems;

public sealed class SharpSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SharpButcherDoafterComplete>(OnDoafterComplete);
        SubscribeLocalEvent<SharpButcherDoafterCancelled>(OnDoafterCancelled);

        SubscribeLocalEvent<SharedButcherableComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnAfterInteract(EntityUid uid, SharpComponent component, AfterInteractEvent args)
    {
        if (args.Target is null || !args.CanReach)
            return;

        TryStartButcherDoafter(uid, args.Target.Value, args.User);
    }

    private void TryStartButcherDoafter(EntityUid knife, EntityUid target, EntityUid user)
    {
        if (!TryComp<SharedButcherableComponent>(target, out var butcher))
            return;

        if (!TryComp<SharpComponent>(knife, out var sharp))
            return;

        if (butcher.Type != ButcheringType.Knife)
            return;

        if (TryComp<MobStateComponent>(target, out var mobState) && !_mobStateSystem.IsDead(target, mobState))
            return;

        if (!sharp.Butchering.Add(target))
            return;

        var doAfter =
            new DoAfterEventArgs(user, sharp.ButcherDelayModifier * butcher.ButcherDelay, default, target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                BroadcastFinishedEvent = new SharpButcherDoafterComplete { User = user, Entity = target, Sharp = knife },
                BroadcastCancelledEvent = new SharpButcherDoafterCancelled { Entity = target, Sharp = knife }
            };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoafterComplete(SharpButcherDoafterComplete ev)
    {
        if (!TryComp<SharedButcherableComponent>(ev.Entity, out var butcher))
            return;

        if (!TryComp<SharpComponent>(ev.Sharp, out var sharp))
            return;

        sharp.Butchering.Remove(ev.Entity);

        if (_containerSystem.IsEntityInContainer(ev.Entity))
            return;

        var spawnEntities = EntitySpawnCollection.GetSpawns(butcher.SpawnedEntities, _robustRandom);
        var coords = Transform(ev.Entity).MapPosition;
        EntityUid popupEnt = default;
        foreach (var proto in spawnEntities)
        {
            // distribute the spawned items randomly in a small radius around the origin
            popupEnt = Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
        }

        var hasBody = TryComp<BodyComponent>(ev.Entity, out var body);

        // only show a big popup when butchering living things.
        var popupType = PopupType.Small;
        if (hasBody)
            popupType = PopupType.LargeCaution;

        _popupSystem.PopupEntity(Loc.GetString("butcherable-knife-butchered-success", ("target", ev.Entity), ("knife", ev.Sharp)),
            popupEnt, ev.User, popupType);

        if (hasBody)
            _bodySystem.GibBody(body!.Owner, body: body);

        _destructibleSystem.DestroyEntity(ev.Entity);
    }

    private void OnDoafterCancelled(SharpButcherDoafterCancelled ev)
    {
        if (!TryComp<SharpComponent>(ev.Sharp, out var sharp))
            return;

        sharp.Butchering.Remove(ev.Entity);
    }

    private void OnGetInteractionVerbs(EntityUid uid, SharedButcherableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (component.Type != ButcheringType.Knife || args.Hands == null)
            return;

        bool disabled = false;
        string? message = null;

        if (args.Using is null || !HasComp<SharpComponent>(args.Using))
        {
            disabled = true;
            message = Loc.GetString("butcherable-need-knife",
                ("target", uid));
        }
        else if (_containerSystem.IsEntityInContainer(uid))
        {
            message = Loc.GetString("butcherable-not-in-container",
                ("target", uid));
            disabled = true;
        }
        else if (TryComp<MobStateComponent>(uid, out var state) && !_mobStateSystem.IsDead(uid, state))
        {
            disabled = true;
            message = Loc.GetString("butcherable-mob-isnt-dead");
        }

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                if (!disabled)
                    TryStartButcherDoafter(args.Using!.Value, args.Target, args.User);
            },
            Message = message,
            Disabled = disabled,
            IconTexture = "/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png",
            Text = Loc.GetString("butcherable-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}

public sealed class SharpButcherDoafterComplete : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid Sharp;
    public EntityUid User;
}

public sealed class SharpButcherDoafterCancelled : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid Sharp;
}
