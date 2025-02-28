using System.Linq;
using System.Numerics;
using Content.Client.Administration.Systems;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
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
    private bool _overlayClassic;
    private bool _overlaySymbols;
    private bool _overlayPlaytime;
    private bool _overlayStartingJob;

    //TODO make this adjustable via GUI
    private readonly ProtoId<RoleTypePrototype>[] _filter =
        ["SoloAntagonist", "TeamAntagonist", "SiliconAntagonist", "FreeAgent"];
    private readonly string _antagLabelClassic = Loc.GetString("admin-overlay-antag-classic");
    private readonly Color _antagColorClassic = Color.OrangeRed;

    public AdminNameOverlay(AdminSystem system, IEntityManager entityManager, IEyeManager eyeManager, IResourceCache resourceCache, EntityLookupSystem entityLookup, IUserInterfaceManager userInterfaceManager)
    {
        IoCManager.InjectDependencies(this);

        _system = system;
        _entityManager = entityManager;
        _eyeManager = eyeManager;
        _entityLookup = entityLookup;
        _userInterfaceManager = userInterfaceManager;
        ZIndex = 200;
        // Setting this to a specific font would break the antag symbols
        _font = resourceCache.NotoStack("Regular", 10);

        _config.OnValueChanged(CCVars.AdminOverlayClassic, (show) => { _overlayClassic = show; }, true);
        _config.OnValueChanged(CCVars.AdminOverlaySymbols, (show) => { _overlaySymbols = show; }, true);
        _config.OnValueChanged(CCVars.AdminOverlayPlaytime, (show) => { _overlayPlaytime = show; }, true);
        _config.OnValueChanged(CCVars.AdminOverlayStartingJob, (show) => { _overlayStartingJob = show; }, true);
    }

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.WorldAABB;

        foreach (var playerInfo in _system.PlayerList)
        {
            var entity = _entityManager.GetEntity(playerInfo.NetEntity);

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
            var lineoffset = new Vector2(0f, 11f) * uiScale;
            var screenCoordinates = _eyeManager.WorldToScreen(aabb.Center +
                                                              new Angle(-_eyeManager.CurrentEye.Rotation).RotateVec(
                                                                  aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);

            var currentOffset = Vector2.Zero;

            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.Username, uiScale, playerInfo.Connected ? Color.Yellow : Color.White);
            currentOffset += lineoffset;

            args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.CharacterName, uiScale, playerInfo.Connected ? Color.Aquamarine : Color.White);
            currentOffset += lineoffset;

            if (!string.IsNullOrEmpty(playerInfo.PlaytimeString) && _overlayPlaytime)
            {
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, playerInfo.PlaytimeString, uiScale, playerInfo.Connected ? Color.Orange : Color.White);
                currentOffset += lineoffset;
            }

            if (!string.IsNullOrEmpty(playerInfo.StartingJob) && _overlayStartingJob)
            {
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, Loc.GetString(playerInfo.StartingJob), uiScale, playerInfo.Connected ? Color.GreenYellow : Color.White);
                currentOffset += lineoffset;
            }

            var symbol = _overlaySymbols ? playerInfo.RoleProto.Symbol : string.Empty;
            if (_overlayClassic && playerInfo.Antag)
            {
                var label = symbol + _antagLabelClassic;
                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, label, uiScale, Color.OrangeRed);
                currentOffset += lineoffset;
            }
            else if (!_overlayClassic && _filter.Contains(playerInfo.RoleProto))
            {
                var label = symbol + Loc.GetString(playerInfo.RoleProto.Name).ToUpper();
                var color = playerInfo.RoleProto.Color;

                args.ScreenHandle.DrawString(_font, screenCoordinates + currentOffset, label, uiScale, color);
                currentOffset += lineoffset;
            }
        }
    }
}
