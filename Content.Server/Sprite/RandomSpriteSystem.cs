using Content.Shared.Decals;
using Content.Shared.Sprite;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Sprite;

public sealed class RandomSpriteSystem: SharedRandomSpriteSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomSpriteComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<RandomSpriteComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomSpriteComponent component, MapInitEvent args)
    {
        if (component.Selected.Count > 0)
            return;

        if (component.Available.Count == 0)
            return;

        var groups = new List<Dictionary<string, (string, string?)>>();
        if (component.GetAllGroups)
        {
            groups = component.Available;
        }
        else
        {
            groups.Add(_random.Pick(component.Available));
        }

        component.Selected.EnsureCapacity(groups.Count);

        foreach (var group in groups)
        {
            foreach (var layer in group)
            {
                Color? color = null;

                if (!string.IsNullOrEmpty(layer.Value.Item2))
                    color = _random.Pick(_prototype.Index<ColorPalettePrototype>(layer.Value.Item2).Colors.Values);

                component.Selected.Add(layer.Key, (layer.Value.Item1, color));
            }
        }

        Dirty(component);
    }

    private void OnGetState(EntityUid uid, RandomSpriteComponent component, ref ComponentGetState args)
    {
        args.State = new RandomSpriteColorComponentState()
        {
            Selected = component.Selected,
        };
    }
}
