using Content.Server.Access;
using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Doors;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using System.Linq;
using static Content.Shared.Doors.DoorComponent;

namespace Content.Server.Doors.Systems;

/// <summary>
/// Used on the server side to manage global access level overrides.
/// </summary>
public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly ConstructionSystem _constructionSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DoorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, DoorComponent door, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool))
            return;

        if (tool.Qualities.Contains(door.PryingQuality))
        {
            TryPryDoor(uid, args.Used, args.User, door);
            args.Handled = true;
            return;
        }

        if (door.Weldable && tool.Qualities.Contains(door.WeldingQuality))
        {
            TryWeldDoor(uid, args.Used, args.User, door);
            args.Handled = true;
        }
    }

    private async void TryWeldDoor(EntityUid target, EntityUid used, EntityUid user, DoorComponent door)
    {
        if (!door.Weldable || door.BeingWelded || door.CurrentlyCrushing.Count > 0)
            return;

        // is the door in a weld-able state?
        if (door.State != DoorState.Closed && door.State != DoorState.Welded)
            return;

        // perform a do-after delay
        door.BeingWelded = true;
        var result = await _toolSystem.UseTool(used, user, target, 3f, 3f, door.WeldingQuality);
        door.BeingWelded = false;

        if (!result || !door.Weldable)
            return;

        if (door.State == DoorState.Closed)
            SetState(target, DoorState.Welded, door);
        else if (door.State == DoorState.Welded)
            SetState(target, DoorState.Closed, door);
    }

    /// <summary>
    ///     Pry open a door. This does not check if the user is holding the required tool.
    /// </summary>
    private async void TryPryDoor(EntityUid target, EntityUid tool, EntityUid user, DoorComponent door)
    {
        if (door.State == DoorState.Welded)
            return;

        var canEv = new BeforeDoorPryEvent(user);
        RaiseLocalEvent(target, canEv, false);

        if (canEv.Cancelled)
            return;

        var modEv = new DoorGetPryTimeModifierEvent();
        RaiseLocalEvent(target, modEv, false);

        var successfulPry = await _toolSystem.UseTool(tool, user, target,
                0f, modEv.PryTimeModifier * door.PryTime, door.PryingQuality);

        if (successfulPry)
        {
            if (door.State == DoorState.Closed)
                StartOpening(target, door);
            else if (door.State == DoorState.Open)
                StartClosing(target, door);
        }
    }

    public override bool HasAccess(EntityUid uid, EntityUid? user = null)
    {
        // TODO move access reader to shared for predicting door opening

        // if there is no "user" we skip the access checks.
        if (user == null || AccessType == AccessTypes.AllowAll)
            return true;

        if (!TryComp(uid, out AccessReader? access))
            return true;

        return AccessType switch
        {
            AccessTypes.AllowAllIdExternal => access.AccessLists.Any(list => list.Contains("External")) || _accessReaderSystem.IsAllowed(access, user.Value),
            AccessTypes.AllowAllNoExternal => !access.AccessLists.Any(list => list.Contains("External")),
            _ => _accessReaderSystem.IsAllowed(access, user.Value)
        };
    }

    protected override void HandleCollide(EntityUid uid, DoorComponent door, StartCollideEvent args)
    {
        // TODO ACCESS READER move access reader to shared and predict door opening/closing
        if (!door.BumpOpen)
            return;

        if (door.State != DoorState.Closed)
            return;

        if (TryComp(args.OtherFixture.Body.Owner, out TagComponent? tags) && tags.HasTag("DoorBumpOpener"))
            TryOpen(uid, door, args.OtherFixture.Body.Owner);

    }

    public override void OnPartialOpen(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics))
            return;

        base.OnPartialOpen(uid, door, physics);

        if (door.ChangeAirtight && TryComp(door.Owner, out AirtightComponent? airtight))
        {
            _airtightSystem.SetAirblocked(airtight, false);
        }

        // Path-finding. Has nothing directly to do with access readers.
        RaiseLocalEvent(new AccessReaderChangeMessage(door.Owner, false));
    }

    public override void OnPartialClose(EntityUid uid, DoorComponent? door = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref door, ref physics))
            return;

        base.OnPartialClose(uid, door, physics);

        // update airtight, if we did not crush something. 
        if (door.ChangeAirtight && door.CurrentlyCrushing.Count != 0 && TryComp(uid, out AirtightComponent? airtight))
            _airtightSystem.SetAirblocked(airtight, true);

        // Path-finding. Has nothing directly to do with access readers.
        RaiseLocalEvent(new AccessReaderChangeMessage(door.Owner, true));
    }

    private void OnMapInit(EntityUid uid, DoorComponent door, MapInitEvent args)
    {
        // Ensure that the construction component is aware of the board container.
        if (TryComp(uid, out ConstructionComponent? construction))
            _constructionSystem.AddContainer(uid, "board", construction);

        // We don't do anything if this is null or empty.
        if (string.IsNullOrEmpty(door.BoardPrototype))
            return;

        var container = uid.EnsureContainer<Container>("board", out var existed);

        /* // TODO ShadowCommander: Re-enable when access is added to boards. Requires map update.
        if (existed)
        {
            // We already contain a board. Note: We don't check if it's the right one!
            if (container.ContainedEntities.Count != 0)
                return;
        }

        var board = Owner.EntityManager.SpawnEntity(_boardPrototype, Owner.Transform.Coordinates);

        if(!container.Insert(board))
            Logger.Warning($"Couldn't insert board {board} into door {Owner}!");
        */
    }
}
