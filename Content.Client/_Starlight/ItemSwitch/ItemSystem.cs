using Content.Shared.Starlight.ItemSwitch;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;

namespace Content.Client._Starlight.ItemSwitch;

public sealed class ItemSwitchSystem : SharedItemSwitchSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemSwitchComponent, AfterAutoHandleStateEvent>(OnChanged);
    }

    private void OnChanged(Entity<ItemSwitchComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateVisuals(ent, ent.Comp.State);

    protected override void UpdateVisuals(Entity<ItemSwitchComponent> ent, string key)
    {
        base.UpdateVisuals(ent, key);
        if (TryComp(ent, out SpriteComponent? sprite) && ent.Comp.States.TryGetValue(key, out var state))
            if (state.Sprite != null)
                sprite.LayerSetSprite(0, state.Sprite);
    }
}
