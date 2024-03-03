
using Content.Server.Librarian.Components;
using Content.Server.Paper;
using Content.Shared.Librarian;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Librarian;


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

        SubscribeLocalEvent<BooksEncryptionComponent, MapInitEvent>(OnMapinit);
        SubscribeLocalEvent<PaperDecryptionHintComponent, MapInitEvent>(OnPaperInit);
    }

    private void OnPaperInit(Entity<PaperDecryptionHintComponent> hint, ref MapInitEvent args)
    {
        if (!TryComp<PaperComponent>(hint, out var paper))
            return;

        for (int i = 0; i < hint.Comp.Hints; i++)
        {
            var pair = GetHint(hint.Comp.Discipline);
            paper.Content += $"\n {Loc.GetString(pair.Item1)} is {Loc.GetString(pair.Item2)}!";
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
