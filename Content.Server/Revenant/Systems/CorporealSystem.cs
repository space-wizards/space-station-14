using Content.Server.GameTicking;
using Content.Shared.Eye;
using Content.Shared.Revenant.Components;
using Content.Shared.Revenant.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Revenant.Systems;

public sealed class CorporealSystem : SharedCorporealSystem
{
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public override void OnStartup(Entity<CorporealComponent> ent, ref ComponentStartup args)
    {
        base.OnStartup(ent, ref args);

        if (TryComp<VisibilityComponent>(ent, out var visibility))
        {
            _visibility.RemoveLayer((ent, visibility), (int) VisibilityFlags.Ghost, false);
            _visibility.AddLayer((ent, visibility), (int) VisibilityFlags.Normal, false);
            _visibility.RefreshVisibility(ent, visibility);
        }
    }

    public override void OnShutdown(Entity<CorporealComponent> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        if (TryComp<VisibilityComponent>(ent, out var visibility) && _ticker.RunLevel != GameRunLevel.PostRound)
        {
            _visibility.AddLayer((ent, visibility), (int) VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer((ent, visibility), (int) VisibilityFlags.Normal, false);
            _visibility.RefreshVisibility(ent, visibility);
        }
    }
}
