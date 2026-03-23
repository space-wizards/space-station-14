using Content.Shared.Paper;
using Content.Shared.Photography;
using Robust.Server.GameObjects;

namespace Content.Server.Photography;

public sealed class PhotographySystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

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

        // maybe this should be cvar value
        const int maxSizeBytes = 100 * 1024;

        if (ev.PhotoBytes.Length > maxSizeBytes)
        {
            Logger.Warning($"Player {args.SenderSession.Name} send many bytes: {ev.PhotoBytes.Length}");
            return;
        }

        var cameraUid = GetEntity(ev.CameraNetUid);

        if (!Exists(cameraUid))
            return;

        var coords = Transform(player.Value).Coordinates;

        var photoEntity = Spawn("PalaroidPaper", coords);

        _metaDataSystem.SetEntityName(photoEntity, _loc.GetString("photography-picture-name"));
        _metaDataSystem.SetEntityDescription(photoEntity, _loc.GetString("photography-picture-description"));

        if (TryComp<PhotographComponent>(photoEntity, out var photoComp))
        {
            photoComp.RawData = ev.PhotoBytes;
            Dirty(photoEntity, photoComp);
        }
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
