using Content.Shared.Heretic;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Heretic;

public sealed partial class HereticCombatMarkSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentShutdown>(OnShutdown);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // i can't think of a better way to do this. everything else has failed
        // god i hate client server i hate client server i hate client server i hate
        foreach (var mark in EntityQuery<HereticCombatMarkComponent>())
        {
            if (!TryComp<SpriteComponent>(mark.Owner, out var sprite))
                continue;

            if (!sprite.LayerMapTryGet(0, out var layer))
                continue;

            sprite.LayerSetState(layer, mark.Path.ToLower());
        }
    }

    private void OnStartup(Entity<HereticCombatMarkComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (sprite.LayerMapTryGet(0, out var l))
        {
            sprite.LayerSetState(l, ent.Comp.Path.ToLower());
            return;
        }

        var rsi = new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/combat_marks.rsi"), ent.Comp.Path.ToLower());
        var layer = sprite.AddLayer(rsi);

        sprite.LayerMapSet(0, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }
    private void OnShutdown(Entity<HereticCombatMarkComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(0, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }
}
