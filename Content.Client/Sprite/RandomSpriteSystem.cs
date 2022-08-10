using Content.Shared.Sprite;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Reflection;

namespace Content.Client.Sprite;

public sealed class RandomSpriteSystem : SharedRandomSpriteSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomSpriteComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, RandomSpriteComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RandomSpriteColorComponentState state)
            return;

        if (state.Selected.Equals(component.Selected))
            return;

        component.Selected.Clear();
        component.Selected.EnsureCapacity(state.Selected.Count);

        foreach (var layer in state.Selected)
        {
            component.Selected.Add(layer.Key, layer.Value);
        }

        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, RandomSpriteComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        foreach (var layer in component.Selected)
        {
            object key;
            if (_reflection.TryParseEnumReference(layer.Key, out var @enum))
            {
                key = @enum;
            }
            else
            {
                key = layer.Key;
            }

            sprite.LayerSetState(key, layer.Value.State);
            sprite.LayerSetColor(key, layer.Value.Color ?? Color.White);
        }
    }
}
