using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Content.Server.Administration.Systems.AdminVerbSystem;

namespace Content.Server.Starlight.Administration.Systems;
public sealed partial class AdminVerbSystem : EntitySystem
{
    [Dependency] private readonly AdminTestArenaSystem _adminTestArenaSystem = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
    }
    private void AddVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Admin))
            return;

        if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
        {
            Verb sendToTestArena = new()
            {
                Text = "Reset test arena",
                Category = VerbCategory.Tricks,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),

                Act = () =>
                {
                    //we technically load the map here, but it doesnt matter, this is safer since the behaviour of this function is garunteed
                    //and reimplementing it would be stupid and unsafe
                    var data = _adminTestArenaSystem.AssertArenaLoaded(player);

                    var _mapManager = _entities.System<SharedMapSystem>();

                    //we need to get the actual map ID, so first get the transform
                    if (!_entities.TryGetComponent(data.Map, out TransformComponent? transform))
                        return;
                    
                    //then get the map ID from the transform
                    MapId mapId = transform.MapID;

                    //call remove map on it
                    _mapManager.DeleteMap(mapId);
                    //_transformSystem.SetCoordinates(args.Target, new EntityCoordinates(data.gridUid ?? data.mapUid, Vector2.One));
                },
                Impact = LogImpact.Medium,
                Message = Loc.GetString("admin-trick-reset-test-arena-description"),
                Priority = (int)TricksVerbPriorities.SendToTestArena,
            };
            args.Verbs.Add(sendToTestArena);
        }
    }
}
