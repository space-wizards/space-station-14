using System.Numerics;
using Content.Client.Administration.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Client.Administration;

internal sealed class AdminNameOverlay : Overlay
{
    private readonly AdminSystem _system;
    private readonly IConfigurationManager _config = IoCManager.Resolve<IConfigurationManager>(); // TODO is this even legal?
    private readonly IEntityManager _entityManager;
    private readonly IEyeManager _eyeManager;
    private readonly EntityLookupSystem _entityLookup;
    private readonly IUserInterfaceManager _userInterfaceManager;
    private readonly Font _font;

    public AdminNameOverlay(AdminSystem system, IEntityManager entityManager, IEyeManager eyeManager, IResourceCache resourceCache, EntityLookupSystem entityLookup, IUserInterfaceManager userInterfaceManager)
    {
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

            //TODO make this adjustable via GUI
            var classic = _config.GetCVar(CCVars.AdminOverlayClassic);
            var label = Loc.GetString(playerInfo.RoleProto.Name).ToUpper();
            var color = playerInfo.RoleProto.Color;
            //TODO make this adjustable via GUI
            var filter = new List<RoleEnum>(){RoleEnum.SoloAntagonist, RoleEnum.TeamAntagonist, RoleEnum.SiliconAntagonist, RoleEnum.FreeAgent};

            if (classic is true)
            {
                label = Loc.GetString("admin-overlay-antag-classic");
                color = Color.OrangeRed;
            }

            if (filter.Contains(playerInfo.RoleProto.RoleRule) && !classic ||
                playerInfo.Antag && classic )
            {
                args.ScreenHandle.DrawString(_font, screenCoordinates + (lineoffset * 2), label, uiScale, color);
            }

            args.ScreenHandle.DrawString(_font, screenCoordinates+lineoffset, playerInfo.Username, uiScale, playerInfo.Connected ? Color.Yellow : Color.White);
            args.ScreenHandle.DrawString(_font, screenCoordinates, playerInfo.CharacterName, uiScale, playerInfo.Connected ? Color.Aquamarine : Color.White);
        }
    }
}
