using Content.Server.GameTicking;
using Content.Shared.Eye;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Revenant.EntitySystems;

/// <summary>
/// Handles server-specific aspects of the corporeal state, particularly managing visibility changes.
/// </summary>
public sealed class CorporealSystem : SharedCorporealSystem
{
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    protected override void OnInit(EntityUid uid, CorporealComponent component, ComponentStartup args)
    {
        base.OnInit(uid, component, args);
        UpdateVisibility(uid, true);
    }

    protected override void OnShutdown(EntityUid uid, CorporealComponent component, ComponentShutdown args)
    {
        base.OnShutdown(uid, component, args);

        if (_gameTicker.RunLevel != GameRunLevel.PostRound)
        {
            UpdateVisibility(uid, false);
        }
    }

    private void UpdateVisibility(EntityUid uid, bool isCorporeal)
    {
        if (!TryComp<VisibilityComponent>(uid, out var visibility))
            return;

        if (isCorporeal)
        {
            _visibilitySystem.RemoveLayer((uid, visibility), (int)VisibilityFlags.Ghost, false);
            _visibilitySystem.AddLayer((uid, visibility), (int)VisibilityFlags.Normal, false);
        }
        else
        {
            _visibilitySystem.AddLayer((uid, visibility), (int)VisibilityFlags.Ghost, false);
            _visibilitySystem.RemoveLayer((uid, visibility), (int)VisibilityFlags.Normal, false);
        }

        _visibilitySystem.RefreshVisibility(uid, visibility);
    }
}
