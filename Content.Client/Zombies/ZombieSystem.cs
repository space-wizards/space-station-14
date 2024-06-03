using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;

namespace Content.Client.Zombies;

public sealed class ZombieSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZombieComponent, CanDisplayStatusIconsEvent>(OnCanDisplayStatusIcons);
        SubscribeLocalEvent<InitialInfectedComponent, CanDisplayStatusIconsEvent>(OnCanDisplayStatusIcons);
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
        if (HasComp<ZombieComponent>(args.User) || HasComp<InitialInfectedComponent>(args.User) || HasComp<ShowZombieIconsComponent>(args.User))
            return;

        if (component.IconVisibleToGhost && HasComp<GhostComponent>(args.User))
            return;

        args.Cancelled = true;
    }

    private void OnCanDisplayStatusIcons(EntityUid uid, InitialInfectedComponent component, ref CanDisplayStatusIconsEvent args)
    {
        if (HasComp<InitialInfectedComponent>(args.User) && !HasComp<ZombieComponent>(args.User))
            return;

        if (component.IconVisibleToGhost && HasComp<GhostComponent>(args.User))
            return;

        args.Cancelled = true;
    }
}
