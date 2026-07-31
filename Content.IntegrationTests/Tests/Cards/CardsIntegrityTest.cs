using Content.IntegrationTests.Fixtures;
using Content.Shared.Cards;
using Content.Shared.Stacks;
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
            var (uid, cards, stack) = SpawnDeck(coords);
            var before = cards.Cards.Count;
            var stackBefore = stack.Count;

            _sCards.TryShuffleCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));
        });
    }

    [Test]
    public async Task FlipPreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, stack) = SpawnDeck(coords);
            var before = cards.Cards.Count;
            var stackBefore = stack.Count;

            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));

            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));

            _sCards.TryFanCards((uid, cards));
            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));

            _sCards.TryFlipCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));
        });
    }

    [Test]
    public async Task FanPreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, stack) = SpawnDeck(coords);
            var before = cards.Cards.Count;
            var stackBefore = stack.Count;

            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));

            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));

            _sCards.TryFlipCards((uid, cards));
            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));

            _sCards.TryFanCards((uid, cards));

            Assert.That(cards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count, Is.EqualTo(stackBefore));
        });
    }

    [Test]
    public async Task SplitPreservesCount()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, stack) = SpawnDeck(coords);
            var before = cards.Cards.Count;
            var stackBefore = stack.Count;

            var split = _sStacks.Split((uid, stack), 20, coords);
            if (split == null)
                Assert.Fail();

            if (!STryComp<CardsComponent>(split, out var splitCards))
                Assert.Fail($"Split entity missing {nameof(CardsComponent)}");
            if (!STryComp<StackComponent>(split, out var splitStack))
                Assert.Fail($"Split entity missing {nameof(StackComponent)}");

            Assert.That(cards.Cards.Count, Is.EqualTo(stack.Count));
            Assert.That(splitCards.Cards.Count, Is.EqualTo(splitStack.Count));
            Assert.That(cards.Cards.Count + splitCards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count + splitStack.Count, Is.EqualTo(stackBefore));
            SQueueDel(split.Value);

            before = cards.Cards.Count;
            stackBefore = stack.Count;

            split = _sStacks.Split((uid, stack), cards.Cards.Count, coords);
            if (split == null)
                Assert.Fail();

            if (!STryComp<CardsComponent>(split, out splitCards))
                Assert.Fail($"Split entity missing {nameof(CardsComponent)}");
            if (!STryComp<StackComponent>(split, out splitStack))
                Assert.Fail($"Split entity missing {nameof(StackComponent)}");

            Assert.That(splitCards.Cards.Count, Is.EqualTo(splitStack.Count));
            Assert.That(splitCards.Cards.Count, Is.EqualTo(before));
            Assert.That(splitStack.Count, Is.EqualTo(stackBefore));

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
            var (uidA, cardsA, stackA) = SpawnDeck(coords, 26);
            var (uidB, cardsB, stackB) = SpawnDeck(coords, 26);
            var before = cardsA.Cards.Count + cardsB.Cards.Count;
            var stackBefore = stackA.Count + stackB.Count;

            if (!_sStacks.TryMergeStacks((uidB, stackB), (uidA, stackA), out var _, amount: 20))
                Assert.Fail();

            Assert.That(cardsA.Cards.Count, Is.EqualTo(stackA.Count));
            Assert.That(cardsB.Cards.Count, Is.EqualTo(stackB.Count));
            Assert.That(cardsA.Cards.Count + cardsB.Cards.Count, Is.EqualTo(before));
            Assert.That(stackA.Count + stackB.Count, Is.EqualTo(stackBefore));

            if (!_sStacks.TryMergeStacks((uidB, stackB), (uidA, stackA), out var _))
                Assert.Fail();

            Assert.That(cardsA.Cards.Count, Is.EqualTo(stackA.Count));
            Assert.That(cardsA.Cards.Count, Is.EqualTo(before));
            Assert.That(stackA.Count, Is.EqualTo(stackBefore));
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
            var (uid, cards, stack) = SpawnDeck(coords);
            var before = cards.Cards.Count;
            var stackBefore = stack.Count;

            if (!_sCards.TryTakeCard((uid, cards), (player, playerXform), cards.Cards[20].CardIndex, out var split))
                Assert.Fail();
            if (split == null)
                Assert.Fail();

            if (!STryComp<CardsComponent>(split, out var splitCards))
                Assert.Fail($"Split entity missing {nameof(CardsComponent)}");
            if (!STryComp<StackComponent>(split, out var splitStack))
                Assert.Fail($"Split entity missing {nameof(StackComponent)}");

            Assert.That(cards.Cards.Count + splitCards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count + splitStack.Count, Is.EqualTo(stackBefore));

            _sStacks.UserSplit((uid, stack), player, cards.Cards.Count - 1);

            Assert.That(cards.Cards.Count + splitCards.Cards.Count, Is.EqualTo(before));
            Assert.That(stack.Count + splitStack.Count, Is.EqualTo(stackBefore));

            if (!_sCards.TryTakeCard((uid, cards), (player, playerXform), cards.Cards[0].CardIndex, out split))
                Assert.Fail();

            Assert.That(splitCards.Cards.Count, Is.EqualTo(before));
            Assert.That(splitStack.Count, Is.EqualTo(stackBefore));

            SQueueDel(split.Value);
        });
    }
}
