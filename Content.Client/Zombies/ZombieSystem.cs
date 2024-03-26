using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;

namespace Content.Client.Zombies;

public sealed class ZombieSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
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
}
