
using Content.Server.BookEncryption.Components;
using Content.Server.Paper;
using Content.Shared.Librarian;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.BookEncryption;


public sealed class BooksEncryptionSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    //I still haven't figured out how to nicely implement centralized data for the system.
    //TO DO: Remove this shit. Sloth, can you give me advise?
    private Entity<BooksEncryptionComponent> _shitcodeData;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BooksEncryptionComponent, MapInitEvent>(OnMapinit, after: new[] { typeof(PaperRandomStorySystem) });
        SubscribeLocalEvent<PaperDecryptionHintComponent, MapInitEvent>(OnPaperInit, after: new[] { typeof(PaperRandomStorySystem) });
    }

    private void OnPaperInit(Entity<PaperDecryptionHintComponent> hint, ref MapInitEvent args)
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

    private void OnMapinit(Entity<BooksEncryptionComponent> encrypt, ref MapInitEvent ev)
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

        _shitcodeData = encrypt;
    }

    // To do: use stack rather than just random, to reduce the chances of frequent repetitions
    public (string, string) GetHint(ProtoId<EncryptedBookDisciplinePrototype> discipline)
    {
        var pairs = _shitcodeData.Comp.KeywordPairs[_proto.Index(discipline)];
        var randomPair = _random.Next(pairs.Count);
        return pairs[randomPair];
    }
}
