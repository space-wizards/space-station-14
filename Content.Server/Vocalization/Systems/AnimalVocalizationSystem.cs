using Content.Server.Speech.Components;
using Content.Server.Speech.Prototypes;
using Content.Server.Vocalization.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <summary>
/// System that handles random vocalization on animals that have an AnimalVocalizationComponent
/// </summary>
public sealed class AnimalVocalizationSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimalVocalizerComponent, TryVocalizeEvent>(OnTryVocalize);
    }

    /// <summary>
    /// Called whenever an entity with a VocalizerComponent and a ReplacementAccentComponent tries to vocalize
    /// </summary>
    private void OnTryVocalize(Entity<AnimalVocalizerComponent> entity, ref TryVocalizeEvent args)
    {
        if (args.Handled)
            return;

        // do not vocalize at all if there is a player controlling this entity
        if (_actor.TryGetSession(entity, out _))
            return;

        // for animals with a replacementAccentPrototype, we need to check whether it actually has those replacements
        if (TryComp<ReplacementAccentComponent>(entity, out var replacementAccent))
        {
            if (!_prototypes.Resolve<ReplacementAccentPrototype>(replacementAccent.Accent, out var prototype))
                return;

            // only use the full replacements. We don't use the word replacements because they're mostly for stuff like
            // the cowboy accent, space italy, etc and we'd have to generate a comprehensible sentence
            if (prototype.FullReplacements is null)
                return;
        }

        // now just use a random-length string
        // random because monkeys can then go OO! or OOOK! or AAAAAAAH!
        args.Message = GenerateRandomString(entity.Comp.MinRandomStringLength, entity.Comp.MaxRandomStringLength, entity.Comp.RandomStringChar);
        args.Handled = true;
    }

    private string GenerateRandomString(int minLength, int maxLength, char useChar)
    {
        var length = _random.Next(minLength, maxLength);

        return new string(useChar, length);
    }
}
