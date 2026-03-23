using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Photography;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Photography;

public sealed class PhotographySystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CameraPhotoCapturedEvent>(OnPhotoCaptured);
        SubscribeLocalEvent<PhotographComponent, BoundUIOpenedEvent>(OnUIOpened);
    }

    private void OnPhotoCaptured(CameraPhotoCapturedEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Handled)
            return;
        ev.Handled = true;

        var player = args.SenderSession.AttachedEntity;
        if (player == null)
            return;

        var cameraUid = GetEntity(ev.CameraNetUid);

        if (!TryComp<CameraComponent>(cameraUid, out var camera))
            return;

        var currentTime = _timing.CurTime;

        if (currentTime < camera.NextPhotoTime)
        {
            return;
        }

        if (camera.CurrentPhotos <= 0)
        {
            return;
        }

        const int maxSizeBytes = 100 * 1024;
        if (ev.PhotoBytes.Length > maxSizeBytes)
        {
            Logger.Warning($"Player {args.SenderSession.Name} sent too many bytes: {ev.PhotoBytes.Length}");
            return;
        }

        camera.CurrentPhotos--;
        camera.NextPhotoTime = currentTime + camera.Cooldown;
        Dirty(cameraUid, camera);

        var coords = Transform(player.Value).Coordinates;
        var photoEntity = Spawn("PalaroidPaper", coords);

        _metaDataSystem.SetEntityName(photoEntity, _loc.GetString("photography-picture-name"));
        _metaDataSystem.SetEntityDescription(photoEntity, _loc.GetString("photography-picture-description"));

        if (TryComp<PhotographComponent>(photoEntity, out var photoComp))
        {
            photoComp.RawData = ev.PhotoBytes;
            Dirty(photoEntity, photoComp);
        }
        _audio.PlayPvs(camera.PrintSound, cameraUid);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player.Value):player} created a new photograph {ToPrettyString(photoEntity):photo} using {ToPrettyString(cameraUid):camera}");
    }

    private void OnUIOpened(Entity<PhotographComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not PolaroidUiKey.Key)
            return;

        UpdateUserInterface(ent.Owner, ent.Comp);
    }

    private void UpdateUserInterface(EntityUid uid, PhotographComponent photo)
    {
        if (!TryComp<PaperComponent>(uid, out var paper))
            return;

        var state = new PolaroidBoundUserInterfaceState(
            photo.RawData,
            paper.Content,
            paper.Mode,
            paper.StampedBy
        );

        _uiSystem.SetUiState(uid, PolaroidUiKey.Key, state);
    }
}
