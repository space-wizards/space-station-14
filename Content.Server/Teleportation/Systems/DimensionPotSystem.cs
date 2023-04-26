using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Server.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Teleportation.Systems;

/// <summary>
/// This handles dimension pot portals and maps.
/// </summary>
public sealed class DimensionPotSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DimensionPotComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DimensionPotComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<DimensionPotComponent, GetVerbsEvent<AlternativeVerb>>(AddTogglePortalVerb);

        _sawmill = Logger.GetSawmill("dimension_pot");
    }

    private void OnStartup(EntityUid uid, DimensionPotComponent comp, ComponentStartup args)
    {
        comp.PocketDimensionMap = _mapMan.CreateMap();
        if (!_mapLoader.TryLoad(comp.PocketDimensionMap, comp.PocketDimensionPath, out var roots))
        {
            _sawmill.Error($"Failed to load pocket dimension map {comp.PocketDimensionPath}");
            QueueDel(uid);
            return;
        }

        if (TryComp<GravityComponent>(_mapMan.GetMapEntityId(comp.PocketDimensionMap), out var gravity))
            gravity.Enabled = true;

        // find the pocket dimension's first grid and put the portal there
        foreach (var root in roots)
        {
            if (!HasComp<MapGridComponent>(root))
                continue;

            // spawn the permanent portal into the pocket dimension, now ready to be used
            var pos = Transform(root).Coordinates;
            comp.DimensionPortal = Spawn(comp.DimensionPortalPrototype, pos);
            _sawmill.Info($"Created pocket dimension on grid {root} of map {comp.PocketDimensionMap}");
            return;
        }

        _sawmill.Error($"Pocket dimension {comp.PocketDimensionPath} had no grids!");
        QueueDel(uid);
    }

    private void OnRemoved(EntityUid uid, DimensionPotComponent comp, ComponentRemove args)
    {
        _sawmill.Info($"Destroying pocket dimension {comp.PocketDimensionMap}");

        if (comp.PocketDimensionMap != MapId.Nullspace)
        {
            // before deleting anything, eject everything in the pocket dimension (that isnt the dimension grid itself)
            var coords = Transform(uid).Coordinates;
            foreach (var gridUid in _mapMan.GetAllMapGrids(comp.PocketDimensionMap))
            {
                // TODO: remove Owner somehow
                foreach (var child in Transform(gridUid.Owner).ChildEntities)
                {
                    // move from the pocket dimension to the pot
                    _transform.SetCoordinates(child, coords);
                }
            }

            // everything inside is probably safe, ready to delete!
            QueueDel(_mapMan.GetMapEntityId(comp.PocketDimensionMap));
            //_mapMan.DeleteMap(comp.PocketDimensionMap);
            // prevent endless deletion
            comp.PocketDimensionMap = MapId.Nullspace;
        }

        if (comp.DimensionPortal != null)
            QueueDel(comp.DimensionPortal.Value);
    }

    private void AddTogglePortalVerb(EntityUid uid, DimensionPotComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("dimension-pot-verb-text"),
            Act = () => HandleActivation(uid, comp, args.User)
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Creates or removes the portals to the pocket dimension.
    /// </summary>
    private void HandleActivation(EntityUid uid, DimensionPotComponent comp, EntityUid user)
    {
        if (comp.PotPortal != null)
        {
            // portal already exists so unlink and delete it
            _link.TryUnlink(comp.DimensionPortal!.Value, comp.PotPortal.Value);
            QueueDel(comp.PotPortal.Value);
            comp.PotPortal = null;
        }
        else
        {
            // create a portal and link it to the pocket dimension
            comp.PotPortal = Spawn(comp.PotPortalPrototype, Transform(uid).Coordinates);
            _link.TryLink(comp.DimensionPortal!.Value, comp.PotPortal.Value, true);
            _transform.SetParent(comp.PotPortal.Value, uid);
        }
    }
}
