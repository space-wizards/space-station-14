using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Reflection;

namespace Content.Server.RandomAppearance;

public class RandomAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAppearanceComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, RandomAppearanceComponent component, ComponentInit args)
    {
        if (_reflectionManager.TryParseEnumReference(component.EnumKeyRaw, out var @enum))
        {
            component.EnumKey = @enum;

            if (TryComp(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData((System.Enum) component.EnumKey, _random.Pick(component.SpriteStates));
            }
        }
        else
        {
            Logger.Error($"RandomAppearance enum key {component.EnumKeyRaw} could not be parsed!");
        }
    }
}
