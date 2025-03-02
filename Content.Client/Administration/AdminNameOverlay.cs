using System.Linq;
using System.Numerics;
using Content.Client.Administration.Systems;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration;

internal sealed class AdminNameOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private readonly AdminSystem _system;
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eyeManager;
    private readonly EntityLookupSystem _entityLookup;
    private readonly IUserInterfaceManager _userInterfaceManager;
    private readonly Font _font;
    //TODO make these be read from cvars (with/after #35538 moves iConfigurationManager to the constructor)
    private float _ghostHideDistance = 300f;
    private float _ghostFadeDistance = 600f;
    private int _maxOverlayStack = 3;
    private float _overlayMergeDistance = 75;

    //TODO make this adjustable via GUI
    private readonly ProtoId<RoleTypePrototype>[] _filter =
        ["SoloAntagonist", "TeamAntagonist", "SiliconAntagonist", "FreeAgent"];
    private readonly string _antagLabelClassic = Loc.GetString("admin-overlay-antag-classic");

    public AdminNameOverlay(AdminSystem system, IEntityManager entityManager, IEyeManager eyeManager, IResourceCache resourceCache, EntityLookupSystem entityLookup, IUserInterfaceManager userInterfaceManager)
    {
        IoCManager.InjectDependencies(this);

        _system = system;
        _entityManager = entityManager;
        _eyeManager = eyeManager;
        _entityLookup = entityLookup;
        _userInterfaceManager = userInterfaceManager;
        ZIndex = 200;
        _font = new VectorFont(resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.WorldAABB;
        var colorDisconnected = Color.White;

        //TODO make this adjustable via GUI
        var classic = _config.GetCVar(CCVars.AdminOverlayClassic);
        var playTime = _config.GetCVar(CCVars.AdminOverlayPlaytime);
        var startingJob = _config.GetCVar(CCVars.AdminOverlayStartingJob);
        var drawnOverlays = new List<(Vector2,Vector2)>() ;

        foreach (var playerInfo in _system.PlayerList)
        {
            var entity = _entityManager.GetEntity(playerInfo.NetEntity);
            var alpha = 1f;

            // Otherwise the entity can not exist yet
            if (entity == null || !_entityManager.EntityExists(entity))
            {
                continue;
            }

            // if not on the same map, continue
            if (_entityManager.GetComponent<TransformComponent>(entity.Value).MapID != args.MapId)
            {
                continue;
            }

            var aabb = _entityLookup.GetWorldAABB(entity.Value);

            // if not on screen, continue
            if (!aabb.Intersects(in viewport))
            {
                continue;
            }

            var uiScale = _userInterfaceManager.RootControl.UIScale;
            var lineoffset = new Vector2(0f, 14f) * uiScale;
            var screenCoordinates = _eyeManager.WorldToScreen(aabb.Center +
                                                              new Angle(-_eyeManager.CurrentEye.Rotation).RotateVec(
                                                                  aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);

            var currentOffset = Vector2.Zero;

            //  Ghosts near the cursor are made transparent/invisible
            //  TODO would be "cheaper" if playerinfo already contained a ghost bool, and ghosts could then be ordered to the bottom of any stack
            if (_entityManager.HasComponent<GhostComponent>(entity))
            {
                var mobPosition = _eyeManager.WorldToScreen(aabb.Center);
                var mousePosition = _userInterfaceManager.MousePositionScaled.Position * uiScale;
                var dist = Vector2.Distance(mobPosition, mousePosition);

                if (dist < _ghostHideDistance)
                    continue;

                alpha = Math.Clamp((dist - _ghostHideDistance) / (_ghostFadeDistance - _ghostHideDistance), 0f, 1f);
                colorDisconnected.A = alpha;
            }

            // If the new overlay textblock is within merge distance of any previous ones
            // merge them into a stack so they don't hide each other
            // additional entries after maximum stack size is reached will be drawn over the last entry
            var stack = drawnOverlays.FindAll(x => Vector2.Distance(x.Item1, screenCoordinates) <= _overlayMergeDistance);
            if (stack.Count > 0)
            {
                screenCoordinates = stack.First().Item1;

                var i = 1;
                foreach (var s in stack)
                {
                    if (i <= _maxOverlayStack - 1)
                        currentOffset = lineoffset + s.Item2 ;
                    i++;
                }
            }

            var color = Color.Yellow;
            color.A = alpha;
            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.Username, uiScale, playerInfo.Connected ? color : colorDisconnected);
            currentOffset += lineoffset;

            color = Color.Aquamarine;
            color.A = alpha;
            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.CharacterName, uiScale, playerInfo.Connected ? color : Color.White);
            currentOffset += lineoffset;

            if (!string.IsNullOrEmpty(playerInfo.PlaytimeString) && playTime)
            {
                color = Color.Orange;
                color.A = alpha;
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.PlaytimeString, uiScale, playerInfo.Connected ? color : colorDisconnected);
                currentOffset += lineoffset;
            }

            if (!string.IsNullOrEmpty(playerInfo.StartingJob) && startingJob)
            {
                color = Color.GreenYellow;
                color.A = alpha;
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, Loc.GetString(playerInfo.StartingJob), uiScale, playerInfo.Connected ? color : colorDisconnected);
                currentOffset += lineoffset;
            }

            if (classic && playerInfo.Antag)
            {
                color = Color.OrangeRed;
                color.A = alpha;
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, _antagLabelClassic, uiScale, color);
                currentOffset += lineoffset;
            }
            else if (!classic && _filter.Contains(playerInfo.RoleProto))
            {
                color =  playerInfo.RoleProto.Color;
                color.A = alpha;
                var label = Loc.GetString(playerInfo.RoleProto.Name).ToUpper();

                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, label, uiScale, color);
                currentOffset += lineoffset;
            }

            //Save the coordinates and size of the text block, to merge with nearby blocks that are too close
            drawnOverlays.Add((screenCoordinates, currentOffset));
        }
    }
}
