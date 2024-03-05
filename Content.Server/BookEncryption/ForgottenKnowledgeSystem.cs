using Content.Server.BookEncryption.Components;
using Content.Server.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.BookEncryption;
using Content.Shared.Verbs;
using Content.Shared.Construction.Components;

namespace Content.Server.BookEncryption;

/// <summary>
/// The underlying system that manages encrypted knowledge
/// </summary>
public sealed class ForgottenKnowledgeSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    //I still haven't figured out how to nicely implement centralized data for the system.
    //Sloth, can you give me advise?
    private Entity<ForgottenKnowledgeComponent> _forgottenKnowledge;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForgottenKnowledgeComponent, MapInitEvent>(OnMapinit, after: new[] { typeof(PaperRandomStorySystem) });
        SubscribeLocalEvent<PaperDecryptionHintComponent, MapInitEvent>(OnHintInit, after: new[] { typeof(PaperRandomStorySystem) });
    }


    private void OnMapinit(Entity<ForgottenKnowledgeComponent> encrypt, ref MapInitEvent ev)
    {
        // Shuffling data
        var disciplines = new List<EncryptedBookDisciplinePrototype>();
        foreach (var item in _proto.EnumeratePrototypes<EncryptedBookDisciplinePrototype>())
        {
            List<(string, string)> pairs = new();
            var keywordCount = item.Keywords.Count;
            for (int i = 0; i < keywordCount; i++)
            {
                var keyword = _random.PickAndTake(item.Keywords);
                var gibberish = _random.PickAndTake(item.Gibberish);
                pairs.Add((keyword, gibberish));
            }

            encrypt.Comp.KeywordPairs.Add(item, pairs);
        }

        _forgottenKnowledge = encrypt;
    }

    private void OnHintInit(Entity<PaperDecryptionHintComponent> hint, ref MapInitEvent args)
    {
        if (!TryComp<PaperComponent>(hint, out var paper))
            return;
        paper.Content += $"\n\n";
        paper.Content += Loc.GetString("lib-book-hint") + "\n";

        var hintCount = _random.Next(hint.Comp.MinHints, hint.Comp.MaxHints);
        for (int i = 0; i < hintCount; i++)
        {
            var pair = GetHint(hint.Comp.Discipline);
            var textVariant = _random.Next(1, 4);
            paper.Content += Loc.GetString($"lib-book-hint-text{textVariant}", ("Gibberish", Loc.GetString(pair.Item2)), ("Keyword", Loc.GetString(pair.Item1))) + "\n";
        }
    }

    // To do: use stack rather than just random, to reduce the chances of frequent repetitions
    public (string, string) GetHint(ProtoId<EncryptedBookDisciplinePrototype> discipline)
    {
        var pairs = _forgottenKnowledge.Comp.KeywordPairs[_proto.Index(discipline)];
        var randomPair = _random.Next(pairs.Count);
        return pairs[randomPair];
    }
}
