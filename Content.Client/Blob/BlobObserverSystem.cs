using Content.Shared.Blob;
using Content.Shared.GameTicking;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;

namespace Content.Client.Blob;

public sealed class BlobObserverSystem : SharedBlobObserverSystem
{
    [Dependency] private readonly ILightManager _lightManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobObserverComponent, ComponentHandleState>(HandleState);
        SubscribeLocalEvent<BlobObserverComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BlobObserverComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);
    }

    private void HandleState(EntityUid uid, BlobObserverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BlobChemSwapComponentState state)
            return;
        component.SelectedChemId = state.SelectedChem;
    }

    private void OnPlayerAttached(EntityUid uid, BlobObserverComponent component, PlayerAttachedEvent args)
    {
        _lightManager.DrawLighting = false;
    }

    private void OnPlayerDetached(EntityUid uid, BlobObserverComponent component, PlayerDetachedEvent args)
    {
        _lightManager.DrawLighting = true;
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _lightManager.DrawLighting = true;
    }
}
