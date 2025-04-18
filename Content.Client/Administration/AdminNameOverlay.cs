using System.Linq;
using System.Numerics;
using Content.Client.Administration.Systems;
using Content.Client.Stylesheets;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration;

internal sealed class AdminNameOverlay : Overlay
{
    private readonly AdminSystem _system;
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eyeManager;
    private readonly EntityLookupSystem _entityLookup;
    private readonly IUserInterfaceManager _userInterfaceManager;
    private readonly SharedRoleSystem _roles;
    private readonly Font _font;
    private readonly Font _fontBold;
    private AdminOverlayAntagFormat _overlayFormat;
    private AdminOverlayAntagSymbolStyle _overlaySymbolStyle;
    private bool _overlayPlaytime;
    private bool _overlayStartingJob;
    private float _ghostFadeDistance;
    private float _ghostHideDistance;
    private int _overlayStackMax;
    private float _overlayMergeDistance;

    //TODO make this adjustable via GUI?
    private readonly ProtoId<RoleTypePrototype>[] _filter =
        ["SoloAntagonist", "TeamAntagonist", "SiliconAntagonist", "FreeAgent"];

    private readonly string _antagLabelClassic = Loc.GetString("admin-overlay-antag-classic");

    public AdminNameOverlay(
        AdminSystem system,
        IEntityManager entityManager,
        IEyeManager eyeManager,
        IResourceCache resourceCache,
        EntityLookupSystem entityLookup,
        IUserInterfaceManager userInterfaceManager,
        IConfigurationManager config,
        SharedRoleSystem roles)
    {
        _system = system;
        _entityManager = entityManager;
        _eyeManager = eyeManager;
        _entityLookup = entityLookup;
        _userInterfaceManager = userInterfaceManager;
        _roles = roles;
        ZIndex = 200;
        // Setting these to a specific ttf would break the antag symbols
        _font = resourceCache.NotoStack();
        _fontBold = resourceCache.NotoStack(variation: "Bold");

        config.OnValueChanged(CCVars.AdminOverlayAntagFormat, (show) => { _overlayFormat = UpdateOverlayFormat(show); }, true);
        config.OnValueChanged(CCVars.AdminOverlaySymbolStyle, (show) => { _overlaySymbolStyle = UpdateOverlaySymbolStyle(show); }, true);
        config.OnValueChanged(CCVars.AdminOverlayPlaytime, (show) => { _overlayPlaytime = show; }, true);
        config.OnValueChanged(CCVars.AdminOverlayStartingJob, (show) => { _overlayStartingJob = show; }, true);
        config.OnValueChanged(CCVars.AdminOverlayGhostHideDistance, (f) => { _ghostHideDistance = f; }, true);
        config.OnValueChanged(CCVars.AdminOverlayGhostFadeDistance, (f) => { _ghostFadeDistance = f; }, true);
        config.OnValueChanged(CCVars.AdminOverlayStackMax, (i) => { _overlayStackMax = i; }, true);
        config.OnValueChanged(CCVars.AdminOverlayMergeDistance, (f) => { _overlayMergeDistance = f; }, true);
    }

    private AdminOverlayAntagFormat UpdateOverlayFormat(string formatString)
    {
        if (!Enum.TryParse<AdminOverlayAntagFormat>(formatString, out var format))
            format = AdminOverlayAntagFormat.Binary;

        return format;
    }

    private AdminOverlayAntagSymbolStyle UpdateOverlaySymbolStyle(string symbolString)
    {
        if (!Enum.TryParse<AdminOverlayAntagSymbolStyle>(symbolString, out var symbolStyle))
            symbolStyle = AdminOverlayAntagSymbolStyle.Off;

        return symbolStyle;
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.WorldAABB;
        var colorDisconnected = Color.White;
        var uiScale = _userInterfaceManager.RootControl.UIScale;
        var lineoffset = new Vector2(0f, 14f) * uiScale;
        var drawnOverlays = new List<(Vector2,Vector2)>() ; // A saved list of the overlays already drawn

        // Get all player positions before drawing overlays, so they can be sorted before iteration
        var sortable = new List<(PlayerInfo, Box2, EntityUid, Vector2)>();
        foreach (var info in _system.PlayerList)
        {
            var entity = _entityManager.GetEntity(info.NetEntity);

            // If entity does not exist or is on a different map, skip
            if (entity == null
                || !_entityManager.EntityExists(entity)
                || _entityManager.GetComponent<TransformComponent>(entity.Value).MapID != args.MapId)
                continue;

            var aabb = _entityLookup.GetWorldAABB(entity.Value);
            // if not on screen, skip
            if (!aabb.Intersects(in viewport))
                continue;

            // Get on-screen coordinates of player
            var screenCoordinates = _eyeManager.WorldToScreen(aabb.Center).Rounded();

            sortable.Add((info, aabb, entity.Value, screenCoordinates));
        }

        // Draw overlays for visible players, starting from the top of the screen
        foreach (var info in sortable.OrderBy(s => s.Item4.Y).ToList())
        {
            var playerInfo = info.Item1;
            var aabb = info.Item2;
            var entity = info.Item3;
            var screenCoordinatesCenter = info.Item4;
            //the center position is kept separately, for simpler position comparison later
            var centerOffset = new Vector2(28f, -18f) * uiScale;
            var screenCoordinates = screenCoordinatesCenter + centerOffset;
            var alpha = 1f;

            //TODO make a smarter system where the starting offset can be modified by the predicted position and size of already-drawn overlays/stacks?
            var currentOffset = Vector2.Zero;

            //  Ghosts near the cursor are made transparent/invisible
            //  TODO would be "cheaper" if playerinfo already contained a ghost bool, this gets called every frame for every onscreen player!
            if (_entityManager.HasComponent<GhostComponent>(entity))
            {
                // We want the map positions here, so we don't have to worry about resolution and such shenanigans
                var mobPosition = aabb.Center;
                var mousePosition = _eyeManager
                    .ScreenToMap(_userInterfaceManager.MousePositionScaled.Position * uiScale)
                    .Position;
                var dist = Vector2.Distance(mobPosition, mousePosition);
                if (dist < _ghostHideDistance)
                    continue;

                alpha = Math.Clamp((dist - _ghostHideDistance) / (_ghostFadeDistance - _ghostHideDistance), 0f, 1f);
                colorDisconnected.A = alpha;
            }

            // If the new overlay text block is within merge distance of any previous ones
            // merge them into a stack so they don't hide each other
            var stack = drawnOverlays.FindAll(x =>
                Vector2.Distance(_eyeManager.ScreenToMap(x.Item1).Position, aabb.Center) <= _overlayMergeDistance);
            if (stack.Count > 0)
            {
                screenCoordinates = stack.First().Item1 + centerOffset;
                // Replacing this overlay's coordinates for the later save with the stack root's coordinates
                // so that other overlays don't try to stack to these coordinates
                screenCoordinatesCenter = stack.First().Item1;

                var i = 1;
                foreach (var s in stack)
                {
                    // additional entries after maximum stack size is reached will be drawn over the last entry
                    if (i <= _overlayStackMax - 1)
                        currentOffset = lineoffset + s.Item2 ;
                    i++;
                }
            }

            // Character name
            var color = Color.Aquamarine;
            color.A = alpha;
            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.CharacterName, uiScale, playerInfo.Connected ? color : colorDisconnected);
            currentOffset += lineoffset;

            // Username
            color = Color.Yellow;
            color.A = alpha;
            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.Username, uiScale, playerInfo.Connected ? color : colorDisconnected);
            currentOffset += lineoffset;

            // Playtime
            if (!string.IsNullOrEmpty(playerInfo.PlaytimeString) && _overlayPlaytime)
            {
                color = Color.Orange;
                color.A = alpha;
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.PlaytimeString, uiScale, playerInfo.Connected ? color : colorDisconnected);
                currentOffset += lineoffset;
            }

            // Job
            if (!string.IsNullOrEmpty(playerInfo.StartingJob) && _overlayStartingJob)
            {
                color = Color.GreenYellow;
                color.A = alpha;
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, Loc.GetString(playerInfo.StartingJob), uiScale, playerInfo.Connected ? color : colorDisconnected);
                currentOffset += lineoffset;
            }

            // Determine antag symbol
            string? symbol;
            switch (_overlaySymbolStyle)
            {
                case AdminOverlayAntagSymbolStyle.Specific:
                    symbol = playerInfo.RoleProto.Symbol;
                    break;
                case AdminOverlayAntagSymbolStyle.Basic:
                    symbol = Loc.GetString("player-tab-antag-prefix");
                    break;
                default:
                case AdminOverlayAntagSymbolStyle.Off:
                    symbol = string.Empty;
                    break;
            }

            // Determine antag/role type name
            string? text;
            switch (_overlayFormat)
            {
                case AdminOverlayAntagFormat.Roletype:
                    color = playerInfo.RoleProto.Color;
                    symbol = _filter.Contains(playerInfo.RoleProto) ? symbol : string.Empty;
                    text = _filter.Contains(playerInfo.RoleProto)
                        ? Loc.GetString(playerInfo.RoleProto.Name).ToUpper()
                        : string.Empty;
                    break;
                case AdminOverlayAntagFormat.Subtype:
                    color = playerInfo.RoleProto.Color;
                    symbol = _filter.Contains(playerInfo.RoleProto) ? symbol : string.Empty;
                    text = _filter.Contains(playerInfo.RoleProto)
                        ? _roles.GetRoleSubtypeLabel(playerInfo.RoleProto.Name, playerInfo.Subtype).ToUpper()
                        : string.Empty;
                    break;
                default:
                case AdminOverlayAntagFormat.Binary:
                    color = Color.OrangeRed;
                    symbol = playerInfo.Antag ? symbol : string.Empty;
                    text = playerInfo.Antag ? _antagLabelClassic : string.Empty;
                    break;
            }

            // Draw antag label
            color.A = alpha;
            var label = !string.IsNullOrEmpty(symbol)
                ? Loc.GetString("player-tab-character-name-antag-symbol", ("symbol", symbol), ("name", text))
                : text;
            args.ScreenHandle.DrawString(_fontBold, screenCoordinates + currentOffset, label, uiScale, color);
            currentOffset += lineoffset;

            //Save the coordinates and size of the text block, for stack merge check
            drawnOverlays.Add((screenCoordinatesCenter, currentOffset));
        }
    }
}
