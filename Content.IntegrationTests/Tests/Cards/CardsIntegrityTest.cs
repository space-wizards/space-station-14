using Content.IntegrationTests.Fixtures;
using Content.Shared.Cards;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Cards;

public sealed partial class CardsTest : GameTest
{
    [Test]
    public async Task ShufflePreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards) = SpawnDeck(coords);
            var before = cards.Cards.Count;

            _sCards.TryShuffleCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
        });
    }

    [Test]
    public async Task FlipAndFanPreserveCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards) = SpawnDeck(coords);
            var before = cards.Cards.Count;

            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFanCards((uid, cards));
            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFlipCards((uid, cards));
            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));

            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
        });
    }

    [Test]
    public async Task SplitPreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards) = SpawnDeck(coords);
            var before = cards.Cards.Count;

            var split = _sCards.SplitDeck((uid, cards), coords, _sCards.MovedCards(cards, 20));
            if (split == null)
                Assert.Fail();

            if (!STryComp<CardsComponent>(split, out var splitCards))
                Assert.Fail($"Split entity missing {nameof(CardsComponent)}");

            Assert.That(cards.Cards.Count + splitCards.Cards.Count, Is.EqualTo(before));
            SQueueDel(split.Value);

            before = cards.Cards.Count;

            split = _sCards.SplitDeck((uid, cards), coords, _sCards.MovedCards(cards, cards.Cards.Count));
            if (split == null)
                Assert.Fail();

            if (!STryComp<CardsComponent>(split, out splitCards))
                Assert.Fail($"Split entity missing {nameof(CardsComponent)}");

            Assert.That(splitCards.Cards.Count, Is.EqualTo(before));

            SQueueDel(split.Value);
        });
    }

    [Test]
    public async Task MergePreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uidA, cardsA) = SpawnDeck(coords, 26);
            var (uidB, cardsB) = SpawnDeck(coords, 26);
            var before = cardsA.Cards.Count + cardsB.Cards.Count;

            if (!_sCards.TryMergeDecks((uidB, cardsB), (uidA, cardsA), out var _, amount: 20))
                Assert.Fail();

            Assert.That(cardsA.Cards.Count + cardsB.Cards.Count, Is.EqualTo(before));

            if (!_sCards.TryMergeDecks((uidB, cardsB), (uidA, cardsA), out var _))
                Assert.Fail();

            Assert.That(cardsA.Cards.Count, Is.EqualTo(before));
        });
    }

    [Test]
    public async Task TakeCardPreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var player = SSpawnAtPosition("MobHuman", coords);
            if (!SEntMan.TryGetComponent<TransformComponent>(player, out var playerXform))
                Assert.Fail($"Player entity missing {nameof(TransformComponent)}");
            var (uid, cards) = SpawnDeck(coords);
            var before = cards.Cards.Count;

            if (!_sCards.TryTakeCard((uid, cards), (player, playerXform), cards.Cards[20].CardIndex, out var split))
                Assert.Fail();
            if (split == null)
                Assert.Fail();

            if (!STryComp<CardsComponent>(split, out var splitCards))
                Assert.Fail($"Split entity missing {nameof(CardsComponent)}");

            Assert.That(cards.Cards.Count + splitCards.Cards.Count, Is.EqualTo(before));

            _sCards.UserSplitDeck((uid, cards), player, cards.Cards.Count - 1);

            Assert.That(cards.Cards.Count + splitCards.Cards.Count, Is.EqualTo(before));

            if (!_sCards.TryTakeCard((uid, cards), (player, playerXform), cards.Cards[0].CardIndex, out split))
                Assert.Fail();

            Assert.That(splitCards.Cards.Count, Is.EqualTo(before));

            SQueueDel(split.Value);
        });
    }
}
