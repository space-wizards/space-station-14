using Robust.Shared.Reflection;

namespace Content.Shared.EntityEffects.Effects.Construction;

public sealed partial class VisualizerDataIntEntityEffectSystem : EntityEffectSystem<AppearanceComponent, VisualizerDataInt>
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    protected override void Effect(Entity<AppearanceComponent> entity, ref EntityEffectEvent<VisualizerDataInt> args)
    {
        if (IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(args.Effect.Key, out var @enum))
        {
            _appearance.SetData(entity.Owner, @enum, args.Effect.Data, entity.Comp);
        }
    }
}

public sealed partial class VisualizerDataInt : EntityEffectBase<VisualizerDataInt>
{
    [DataField]
    public string Key = string.Empty;
    [DataField]
    public int Data;
}
