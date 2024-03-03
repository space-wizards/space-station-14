
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
        var disciplines = new List<EncryptedBookDisciplinePrototype>();
        foreach (var item in _proto.EnumeratePrototypes<EncryptedBookDisciplinePrototype>())
        {
            var keywordCount = item.Keywords.Count;
            for (int i = 0; i < keywordCount; i++)
            {
                encrypt.Comp.KeywordPair.Add(_random.PickAndTake(item.Keywords), _random.PickAndTake(item.Gibberish));
            }
        }
        foreach (var item in encrypt.Comp.KeywordPair)
        {
            Log.Warning($"Now {Loc.GetString(item.Key)} is {Loc.GetString(item.Value)}.");
        }
    }
}
