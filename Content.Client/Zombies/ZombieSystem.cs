using System.Linq;
using Content.Client.Antag;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Client.Zombies;

public sealed class ZombieSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _zombieIconGhostVisibility;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.ZombieIconsVisibleToGhosts, OnZombieIconGhostVisibilityChanged, true);
        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZombieComponent, CanDisplayStatusIconsEvent>(OnCanDisplayStatusIcons);
    }

    private void OnZombieIconGhostVisibilityChanged(bool value) => _zombieIconGhostVisibility = value;

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.ZombieIconsVisibleToGhosts, OnZombieIconGhostVisibilityChanged);
    }

    private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
    {
        if (HasComp<HumanoidAppearanceComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            sprite.LayerSetColor(i, component.SkinColor);
        }
    }

    /// <summary>
    /// Determines whether a player should be able to see the StatusIcon for zombies.
    /// </summary>
    private void OnCanDisplayStatusIcons(EntityUid uid, ZombieComponent component, ref CanDisplayStatusIconsEvent args)
    {
        if (HasComp<ZombieComponent>(args.User) || HasComp<ShowZombieIconsComponent>(args.User))
            return;

        if (_zombieIconGhostVisibility && HasComp<GhostComponent>(args.User))
            return;

        args.Cancelled = true;
    }
}
