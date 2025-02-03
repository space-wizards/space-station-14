using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed class ButcherableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ButcherableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ButcherableComponent, ButcherDoafterEvent>(OnButcherDoafter);
        SubscribeLocalEvent<ButcherableComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    // Handles interactions with an object in hand, such as using a knife to butcher an animal
    private void OnInteractUsing(Entity<ButcherableComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !_interactionSystem.InRangeUnobstructed(entity.Owner, args.User))
            return;

        if (TryStartButcherDoafter(entity, args.User, args.Used))
            args.Handled = true;
    }

    private bool TryStartButcherDoafter(Entity<ButcherableComponent> entity, EntityUid user, EntityUid used)
    {
        if (TryComp<MobStateComponent>(entity, out var mobState) && !_mobStateSystem.IsDead(entity, mobState))
            return false;

        if (!TryComp<ToolComponent>(used, out var usedToolComponent))
            return false;

        if (!_toolSystem.HasQuality(used, entity.Comp.ToolQuality, usedToolComponent))
            return false;

        _popupSystem.PopupClient(Loc.GetString("butcherable-knife-butcher-start", ("target", entity)), user, user);

        if (!_toolSystem.UseTool(used, user, entity, usedToolComponent.SpeedModifier * entity.Comp.ButcherDelay, entity.Comp.ToolQuality, new ButcherDoafterEvent()))
            return false;

        return true;
    }

    private void OnButcherDoafter(Entity<ButcherableComponent> entity, ref ButcherDoafterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (_containerSystem.IsEntityInContainer(entity))
        {
            args.Handled = true;
            return;
        }

        var entityCoords = Transform(entity).Coordinates;
        var hasBody = TryComp<BodyComponent>(entity, out var body);

        if (_net.IsServer)
        {
            var spawnEntities = EntitySpawnCollection.GetSpawns(entity.Comp.SpawnedEntities, _robustRandom);
            var coords = _transform.GetMapCoordinates(entity);
            foreach (var proto in spawnEntities)
            {
                // distribute the spawned items randomly in a small radius around the origin
                Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
            }

            if (hasBody)
                _bodySystem.GibBody(entity, body: body);

            _destructibleSystem.DestroyEntity(entity);
        }

        // only show a big popup when butchering living things.
        var popupType = hasBody ? PopupType.LargeCaution : PopupType.Small;

        if (args.Used != null)
        {
            _popupSystem.PopupClient(
                Loc.GetString("butcherable-knife-butchered-success", ("target", entity), ("knife", args.Used)),
                entityCoords,
                args.User,
                popupType);
        }

        args.Handled = true;

        _adminLogger.Add(LogType.Gib,
            $"{EntityManager.ToPrettyString(args.User):user} " +
            $"has butchered {EntityManager.ToPrettyString(args.Target):target} " +
            $"with {EntityManager.ToPrettyString(args.Used):knife}");
    }

    private void OnGetInteractionVerbs(EntityUid entityUid, ButcherableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!_interactionSystem.InRangeUnobstructed(entityUid, args.User))
            return;

        var disabled = false;
        string? message = null;

        // entities with no hands and no butchering tool quality don't even get the privilege of seeing the butcher option
        if (!TryComp<ToolComponent>(args.User, out var userToolComp) && !_toolSystem.HasQuality(args.User, component.ToolQuality, userToolComp) && args.Hands == null)
            return;

        // entities with hands holding non-butchering items get a disabled butcher option
        if (!TryComp<ToolComponent>(args.Using, out var usingToolComp) && args.Hands != null)
        {
            if (args.Using == null || !_toolSystem.HasQuality(args.Using.Value, component.ToolQuality, usingToolComp))
            {
                disabled = true;
                message = Loc.GetString("butcherable-need-knife",
                    ("target", entityUid));
            }
        }
        // we don't like butchering things when the entity is inside a container for some reason??
        // I haven't investigated if this is even necessary, but it's been around for a while
        else if (_containerSystem.IsEntityInContainer(entityUid))
        {
            disabled = true;
            message = Loc.GetString("butcherable-not-in-container",
                ("target", entityUid));
        }
        // we don't want to butcher something that's alive
        else if (TryComp<MobStateComponent>(entityUid, out var state) && !_mobStateSystem.IsDead(entityUid, state))
        {
            disabled = true;
            message = Loc.GetString("butcherable-mob-isnt-dead");
        }

        // preferably we butcher with the object in the user's hand
        // otherwise we try butchering with the user themselves
        EntityUid butcheringObject = default;
        if (usingToolComp != null)
            butcheringObject = args.Using!.Value;
        else if (userToolComp != null)
            butcheringObject = args.User;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                if (!disabled)
                    TryStartButcherDoafter((entityUid, component), args.User, butcheringObject);
            },
            Message = message,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("butcherable-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}
