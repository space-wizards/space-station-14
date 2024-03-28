using Content.Shared.Humanoid;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityList;

/// <summary>
/// System for modifying lists of entity prototypes based on presets and contextual information.
/// </summary>
public sealed partial class EntityOverrideSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static EntityUid? _context = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityOverrideMarkContextEvent>(OnEntityOverrideMarkContext);
        SubscribeLocalEvent<EntityOverrideClearContextEvent>(OnEntityOverrideClearContext);
        SubscribeLocalEvent<EntityOverrideApplyEvent>(OnApplyEntityOverrides);
    }

    private void OnEntityOverrideMarkContext(ref EntityOverrideMarkContextEvent args)
    {
        _context = args.Context;
    }

    private void OnEntityOverrideClearContext(ref EntityOverrideClearContextEvent args)
    {
        _context = null;
    }

    private void OnApplyEntityOverrides(ref EntityOverrideApplyEvent args)
    {
        foreach (var presetId in args.Presets)
        {
            if (!_prototypeManager.TryIndex(presetId, out var preset))
                continue;

            if (preset.Species != null && _entityManager.TryGetComponent<HumanoidAppearanceComponent>(_context, out var humanoidAppearance))
            {
                if (preset.Species != humanoidAppearance.Species)
                    continue;
            }

            if (preset.Job != null && _entityManager.TryGetComponent<JobComponent>(_context, out var job))
            {
                if (job.Prototype != null && preset.Job != job.Prototype)
                    continue;
            }

            var replacements = 0;

            for (var p = 0; p < args.Prototypes.Count; p++)
            {
                if (preset.Target != args.Prototypes[p])
                    continue;

                args.Prototypes[p] = preset.Pick(_random, args.Key);

                if (++replacements >= preset.MaxReplacements)
                    break;
            }
        }
    }
}

/// <summary>
/// Event raised to configure a list of entity prototypes according to one or more presets.
/// </summary>
/// <param name="Prototypes">List of prototypes in question.</param>
/// <param name="Presets">List of presets to determine which contextual checks to perform, which prototypes to replace and with what.</param>
/// <param name="Key">Key to use if the preset requires picking from a dictionary. Allows some presets to respond differently based on what system is applying them.</param>
[ByRefEvent]
public readonly record struct EntityOverrideApplyEvent(in List<string> Prototypes, List<ProtoId<EntityOverridePrototype>> Presets, string? Key = null);

/// <summary>
/// Event raised to mark an entity as a target for contextual checks. This is required since systems like <see cref="StorageFill"/> operate on <see cref="MapInitEvent"/>.
/// </summary>
/// <param name="Context">Entity in question.</param>
[ByRefEvent]
public readonly struct EntityOverrideMarkContextEvent(EntityUid context)
{
    public readonly EntityUid Context = context;
}

/// <summary>
/// Event raised to clear the entity recored by <see cref="EntityOverrideSystem"/> and omit all contextual checks.
/// </summary>
[ByRefEvent]
public readonly struct EntityOverrideClearContextEvent();
