using Robust.Shared.Random;

namespace Content.Server.RandomAppearance;

public sealed class RandomAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAppearanceComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, RandomAppearanceComponent component, ComponentInit args)
    {
        if (TryComp(uid, out AppearanceComponent? appearance) && component.EnumKey != null)
        {
            _appearanceSystem.SetData(appearance.Owner, component.EnumKey, _random.Pick(component.SpriteStates), appearance);
        }
    }
}
