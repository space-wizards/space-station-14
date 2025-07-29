using Content.Server.Speech.Components;
using Content.Server.Speech.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <summary>
/// System that handles random vocalization on entities that have a ReplacementAccentComponent. Used in animals that
/// vocalize randomly.
/// </summary>
public sealed class ReplacementAccentVocalizationSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplacementAccentComponent, TryVocalizeEvent>(OnTryVocalize);

        // monkeys are SPECIAL apparently
        SubscribeLocalEvent<MonkeyAccentComponent, TryVocalizeEvent>(OnTryVocalizeMonkey);
    }

    /// <summary>
    /// Called whenever an entity with a VocalizerComponent and a ReplacementAccentComponent tries to vocalize
    /// </summary>
    private void OnTryVocalize(Entity<ReplacementAccentComponent> entity, ref TryVocalizeEvent args)
    {
        if (args.Handled)
            return;

        // do not vocalize at all if there is a player controlling this entity
        if (_actor.TryGetSession(entity, out _))
            return;

        if (!_prototypes.TryIndex<ReplacementAccentPrototype>(entity.Comp.Accent, out var prototype))
            return;

        if (prototype.FullReplacements is null)
            return;

        var replacement = _random.Pick(prototype.FullReplacements);

        args.Message = replacement;
        args.Handled = true;
    }

    /// <summary>
    /// Fired whenever an entity with a VocalizerComponent and a MonkeyAccentComponent tries to vocalize
    /// Monkeys are the one mob that can be expected to be noisy that has an accent that isn't just a ReplacementAccent.
    /// Instead, they use a more involved way of replacing input.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnTryVocalizeMonkey(Entity<MonkeyAccentComponent> entity, ref TryVocalizeEvent args)
    {
        if (args.Handled)
            return;

        // do not vocalize at all if there is a player controlling this entity
        if (_actor.TryGetSession(entity, out _))
            return;

        // quickest way I can think of to generate some arbitrary length string without hardcoding anything or exposing
        // a useless variable somewhere
        args.Message = _random.Next().ToString();
        args.Handled = true;
    }
}
