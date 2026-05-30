using Content.Shared.EntityEffects;
using Content.Shared.Tools.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tools.Systems;

/// <summary>
/// This system implements the behavior of <see cref="EffectOnToolInteractionComponent"/>. It just translates
/// </summary>
public sealed partial class EffectOnToolInteractionSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;

    [Dependency] private EntityQuery<SimpleToolInteractionComponent> _simpleToolInteractionQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectOnToolInteractionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EffectOnToolInteractionComponent, ToolInteractionEvent>(OnToolInteraction);
    }

    private void OnComponentStartup(Entity<EffectOnToolInteractionComponent> ent, ref ComponentStartup args)
    {
        if (!_simpleToolInteractionQuery.HasComp(ent))
        {
            AddComp<SimpleToolInteractionComponent>(ent);
            Log.Warning($"{ToPrettyString(ent)} has {nameof(EffectOnToolInteractionComponent)} but is " +
                        $"missing {nameof(SimpleToolInteractionComponent)}! {nameof(EffectOnToolInteractionComponent)} " +
                        $"requires {nameof(SimpleToolInteractionComponent)} to work, so ensure both are added.");
        }
    }

    private void OnToolInteraction(Entity<EffectOnToolInteractionComponent> ent, ref ToolInteractionEvent args)
    {
        List<EntityEffect> targetEffects = [];
        List<EntityEffect> toolEffects = [];
        foreach (ProtoId<ToolQualityPrototype> toolQuality in args.Tool.Comp.Qualities)
        {
            if (!ent.Comp.Effects.TryGetValue(toolQuality, out var effects))
                continue;

            foreach (var effect in effects)
            {
                targetEffects.AddRange(effect.Target ?? []);
                toolEffects.AddRange(effect.Tool ?? []);
            }
        }

        _entityEffects.ApplyEffects(ent, targetEffects.ToArray(), user: args.User);
        _entityEffects.ApplyEffects(args.Tool, toolEffects.ToArray(), user: args.User);
    }
}
