using Content.Client.DeadSpace.StationAI.UI;
using Content.Shared.DeadSpace.StationAI.UI;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly IOverlayManager _overlayMgr = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private StationAiOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        InitializeAirlock();
        InitializePowerToggle();

        SubscribeLocalEvent<StationAiOverlayComponent, LocalPlayerAttachedEvent>(OnAiAttached);
        SubscribeLocalEvent<StationAiOverlayComponent, LocalPlayerDetachedEvent>(OnAiDetached);
        SubscribeLocalEvent<StationAiOverlayComponent, ComponentInit>(OnAiOverlayInit);
        SubscribeLocalEvent<StationAiOverlayComponent, ComponentRemove>(OnAiOverlayRemove);

        // SubscribeLocalEvent<StationAiOverlayComponent, AfterAutoHandleStateEvent>(OnCamUpdate);
    }

    private void OnAiOverlayInit(Entity<StationAiOverlayComponent> ent, ref ComponentInit args)
    {
        var attachedEnt = _player.LocalEntity;

        if (attachedEnt != ent.Owner)
            return;

        AddOverlay();
    }

    private void OnAiOverlayRemove(Entity<StationAiOverlayComponent> ent, ref ComponentRemove args)
    {
        var attachedEnt = _player.LocalEntity;

        if (attachedEnt != ent.Owner)
            return;

        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (_overlay != null)
            return;

        _overlay = new StationAiOverlay();
        _overlayMgr.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMgr.RemoveOverlay(_overlay);
        _overlay = null;
    }

    private void OnAiAttached(Entity<StationAiOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    private void OnAiDetached(Entity<StationAiOverlayComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMgr.RemoveOverlay<StationAiOverlay>();
    }

    // private void OnCamUpdate(Entity<StationAiOverlayComponent> ent, ref AfterAutoHandleStateEvent args)
    // {
    //     if (!TryComp<UserInterfaceComponent>(ent, out var userInterface) ||
    //         !userInterface.ClientOpenInterfaces.TryGetValue(AICameraListUiKey.Key, out var ui1) ||
    //         ui1 is not AICameraListBoundUserInterface ui)
    //     {
    //         return;
    //     }
    //     ui.Update();
    // }
}
