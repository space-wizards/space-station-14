
using Content.Server.GameTicking.Events;
using Content.Server.Librarian.Components;
using Content.Shared.Librarian;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Librarian;


public sealed class BooksEncryptionSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BooksEncryptionComponent, MapInitEvent>(OnMapinit);
    }

    private void OnMapinit(Entity<BooksEncryptionComponent> encrypt, ref MapInitEvent ev)
    {
        // Reshuffling
        var disciplines = new List<EncryptedBookDisciplinePrototype>();
        foreach (var item in _proto.EnumeratePrototypes<EncryptedBookDisciplinePrototype>())
        {
            var pairs = new Dictionary<string, string>();
            var keywordCount = item.Keywords.Count;
            Log.Warning($"Discipline: {Loc.GetString(item.Name)}");
            for (int i = 0; i < keywordCount; i++)
            {
                var keyword = _random.PickAndTake(item.Keywords);
                var gibberish = _random.PickAndTake(item.Gibberish);
                pairs.Add(keyword, gibberish);
                Log.Warning($"Now {Loc.GetString(keyword)} is {Loc.GetString(gibberish)}.");
            }

            encrypt.Comp.KeywordPairs.Add(item, pairs);
        }
    }
}
