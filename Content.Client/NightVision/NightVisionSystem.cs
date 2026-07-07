using Content.Client.Overlays;
using Content.Shared.GameTicking;
using Content.Shared.NightVision;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.NightVision;

/// <inheritdoc/>
public sealed partial class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IPlayerManager _player = default!;

    private NightVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new NightVisionOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RefreshOverlay(args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        Deactivate(_player.LocalEntity);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<NightVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay(ent);
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        var localPlayer = _player.LocalSession?.AttachedEntity;
        if (localPlayer != null)
            Deactivate(localPlayer.Value);
    }

<<<<<<< Updated upstream
    private void Update(EntityUid entity, List<Entity<NightVisionComponent>> entities)
=======
    /// <summary>
    /// Update the state of the overlay. Add/remove/modify based on <see cref="NightVisionComponent"/>s if any.
    /// </summary>
    /// <param name="entity">The entity to have an overlay added/removed from.</param>
    /// <param name="components">A list of <see cref="NightVisionComponent"/>s if any.</param>
    private void Update(EntityUid entity, List<NightVisionComponent> components)
>>>>>>> Stashed changes
    {
        if (entity != _player.LocalSession?.AttachedEntity)
            return;

        // Find any prioritized components
        NightVisionComponent? nvision = null;
<<<<<<< Updated upstream
        var bestNoise = float.MaxValue;
        foreach (var ent in entities)
=======
        foreach (var comp in components)
>>>>>>> Stashed changes
        {
            if (!ent.Comp.Enabled)
                continue;

<<<<<<< Updated upstream
            if (ent.Comp.RelayOverlay == (ent.Owner == entity))
                continue;

            if (ent.Comp.Prioritized)
            {
                nvision = ent.Comp;
                break;
            }

            var noise = ent.Comp.NoiseAmount * ent.Comp.NoiseMultiplier;
            if (noise < bestNoise)
            {
                nvision = ent.Comp;
                bestNoise = noise;
            }
=======
            nvision = comp;

            // Take the first priority component
            if (comp.Prioritized)
                break;
>>>>>>> Stashed changes
        }

        // There is no active night vision components, so we disable the overlay.
        if (nvision == null)
        {
            Deactivate(entity);
            return;
        }

        // Relay all the settings from the component.
        _overlay.SetParameters(
            nvision.LightingColor,
            nvision.PhosphorColor,
            nvision.GoggleEffect,
            nvision.ViewCircleRadius,
            nvision.ViewCircleSpacing,
            nvision.ViewCircleCount,
            nvision.Amplification);

        if (!_overlayMan.HasOverlay<NightVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    private void Deactivate(EntityUid? ent)
    {
        if (ent != _player.LocalSession?.AttachedEntity)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    protected override void RefreshOverlay(EntityUid target)
    {
        if (target != _player.LocalSession?.AttachedEntity)
            return;

        var ev = new RefreshNightVisionEvent();
        RaiseLocalEvent(target, ref ev);

        if (ev.Entities.Count > 0)
            Update(target, ev.Entities);
        else
            Deactivate(target);
    }
}
