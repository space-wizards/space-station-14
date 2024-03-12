using Content.Server.Body.Systems;
using Content.Server.Kitchen.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Kitchen;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems;

public sealed class SharpSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SharpComponent, SharpDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<ButcherableComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnAfterInteract(EntityUid uid, SharpComponent component, AfterInteractEvent args)
    {
        if (args.Target is null || !args.CanReach)
            return;

        TryStartButcherDoafter(uid, args.Target.Value, args.User);
    }

    private void TryStartButcherDoafter(EntityUid knife, EntityUid target, EntityUid user)
    {
        if (!TryComp<ButcherableComponent>(target, out var butcher))
            return;

        if (!TryComp<SharpComponent>(knife, out var sharp))
            return;

        if (butcher.Type != ButcheringType.Knife)
        {
            _popupSystem.PopupEntity(Loc.GetString("butcherable-different-tool", ("target", target)), knife, user);
            return;
        }

        if (TryComp<MobStateComponent>(target, out var mobState) && !_mobStateSystem.IsDead(target, mobState))
            return;

        if (!sharp.Butchering.Add(target))
            return;

        var doAfter =
            new DoAfterArgs(EntityManager, user, sharp.ButcherDelayModifier * butcher.ButcherDelay, new SharpDoAfterEvent(), knife, target: target, used: knife)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };
        _doAfterSystem.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(EntityUid uid, SharpComponent component, DoAfterEvent args)
    {
        if (args.Handled || !TryComp<ButcherableComponent>(args.Args.Target, out var butcher))
            return;

        if (args.Cancelled)
        {
            component.Butchering.Remove(args.Args.Target.Value);
            return;
        }

        component.Butchering.Remove(args.Args.Target.Value);

        if (_containerSystem.IsEntityInContainer(args.Args.Target.Value))
        {
            args.Handled = true;
            return;
        }

        var spawnEntities = EntitySpawnCollection.GetSpawns(butcher.SpawnedEntities, _robustRandom);
        var coords = Transform(args.Args.Target.Value).MapPosition;
        EntityUid popupEnt = default!;
        foreach (var proto in spawnEntities)
        {
            // distribute the spawned items randomly in a small radius around the origin
            popupEnt = Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
        }

        var hasBody = TryComp<BodyComponent>(args.Args.Target.Value, out var body);

        // only show a big popup when butchering living things.
        var popupType = PopupType.Small;
        if (hasBody)
            popupType = PopupType.LargeCaution;

        _popupSystem.PopupEntity(Loc.GetString("butcherable-knife-butchered-success", ("target", args.Args.Target.Value), ("knife", uid)),
            popupEnt, args.Args.User, popupType);

        if (hasBody)
            _bodySystem.GibBody(args.Args.Target.Value, body: body);

        _destructibleSystem.DestroyEntity(args.Args.Target.Value);

        args.Handled = true;

        _adminLogger.Add(LogType.Gib,
            $"{EntityManager.ToPrettyString(args.User):user} " +
            $"has butchered {EntityManager.ToPrettyString(args.Target):target} " +
            $"with {EntityManager.ToPrettyString(args.Used):knife}");
    }

    private void OnGetInteractionVerbs(EntityUid uid, ButcherableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (component.Type != ButcheringType.Knife || args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        bool disabled = false;
        string? message = null;

        if (!HasComp<SharpComponent>(args.Using))
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
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("butcherable-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}
