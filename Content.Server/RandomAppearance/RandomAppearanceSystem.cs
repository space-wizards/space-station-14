using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.RandomAppearance;

public sealed partial class RandomAppearanceSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAppearanceComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, RandomAppearanceComponent component, ComponentInit args)
    {
        if (TryComp(uid, out AppearanceComponent? appearance) && component.EnumKey != null)
        {
            _appearance.SetData(uid, component.EnumKey, _random.Pick(component.SpriteStates), appearance);
        }
    }
}
