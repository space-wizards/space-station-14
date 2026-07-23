using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Cards;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Cards;

public sealed partial class CardsTest
{
    [SidedDependency(Side.Server)]
    private readonly SharedCardSystem _sCards = null!;

    [SidedDependency(Side.Server)]
    private readonly SharedStackSystem _sStacks = null!;

    [SidedDependency(Side.Server)]
    private readonly IComponentFactory _sCompFact = null!;

    private const string CardsProtoId = "cardDeck";

    /// <summary>
    /// Helper to spawn a card deck.
    /// Can spawn a deck with some cards removed. Don't use <c>removed</c> in tests unless splitting is checked separately.
    /// </summary>
    private (EntityUid uid, CardsComponent cards, StackComponent stack) SpawnDeck(
        EntityCoordinates coords,
        int removed = 0
    )
    {
        var uid = SSpawnAtPosition(CardsProtoId, coords);
        if (!SEntMan.TryGetComponent<CardsComponent>(uid, out var cards))
            Assert.Fail("Spawned cardDeck is missing CardsComponent");
        if (!SEntMan.TryGetComponent<StackComponent>(uid, out var stack))
            Assert.Fail("Spawned cardDeck is missing StackComponent");
        if (removed != 0)
        {
            var splitA = _sStacks.Split((uid, stack!), removed, coords);
            SQueueDel(splitA!.Value);
        }
        return (uid, cards!, stack!);
    }

    [Test]
    public async Task TryTakeCardWithInvalidInxFails()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var player = SSpawnAtPosition("MobHuman", coords);
            var (uid, cards, stack) = SpawnDeck(coords);
            var cardsBefore = cards.Cards.Count;

            if (!SEntMan.TryGetComponent<TransformComponent>(player, out var playerXform))
                Assert.Fail($"Missing player {nameof(TransformComponent)}");

            var result = _sCards.TryTakeCard((uid, cards), (player, playerXform), int.MaxValue, out var split);

            Assert.That(result, Is.False);

            Assert.That(cards.Cards.Count, Is.EqualTo(cardsBefore));
        });
    }

    [Test]
    public async Task UnflippedSplitTakesFromFront()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, stack) = SpawnDeck(coords);
            Assert.That(cards.Flipped, Is.False);

            var originalFirstCards = cards.Cards.Take(5).Select(c => c.CardInx).ToList();

            var split = _sStacks.Split((uid, stack), 5, coords);
            Assert.That(split, Is.Not.Null);

            if (!SEntMan.TryGetComponent<CardsComponent>(split!.Value, out var splitCards))
                Assert.Fail($"Missing {nameof(CardsComponent)} on split");

            var splitInxes = splitCards!.Cards.Select(c => c.CardInx).ToList();
            Assert.That(
                splitInxes,
                Is.EquivalentTo(originalFirstCards),
                "Unflipped deck: split should take the first N cards (by CardInx)"
            );
        });
    }

    [Test]
    public async Task FlippedSplitTakesFromBack()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, stack) = SpawnDeck(coords);
            var total = cards.Cards.Count;
            var originalLastCards = cards.Cards.TakeLast(5).Select(c => c.CardInx).ToList();

            _sCards.TryFlipCards((uid, cards));

            var split = _sStacks.Split((uid, stack), 5, coords);
            Assert.That(split, Is.Not.Null);

            if (!SEntMan.TryGetComponent<CardsComponent>(split!.Value, out var splitCards))
                Assert.Fail($"Missing {nameof(CardsComponent)} on split");

            var splitInxes = splitCards!.Cards.Select(c => c.CardInx).ToList();
            Assert.That(
                splitInxes,
                Is.EquivalentTo(originalLastCards),
                "Flipped deck: split should take the last N cards (by CardInx)"
            );
        });
    }

    [Test]
    public async Task SingleCardDeckDoesNotFan()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, stack) = SpawnDeck(coords);

            var leaveOne = stack.Count - 1;
            var split = _sStacks.Split((uid, stack), leaveOne, new EntityCoordinates(uid, Vector2.Zero));
            Assert.That(split, Is.Not.Null);
            Assert.That(stack.Count, Is.EqualTo(1));

            _sCards.TryFanCards((uid, cards));

            var state = _sCards.GetCardListVisualState(cards);
            Assert.That(
                state.Count,
                Is.EqualTo(1),
                "Visual state of a single-card deck should show exactly 1 card even when Fanned"
            );
        });
    }

    [Test]
    public async Task CardInxesAreUniqueAfterSpawn()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (_, cardsA, _) = SpawnDeck(coords);
            var (_, cardsB, _) = SpawnDeck(coords);
            var (_, cardsC, _) = SpawnDeck(coords);
            var (_, cardsD, _) = SpawnDeck(coords);
            var (_, cardsE, _) = SpawnDeck(coords);
            var allCardLists = cardsA
                .Cards.Concat(cardsB.Cards)
                .Concat(cardsC.Cards)
                .Concat(cardsD.Cards)
                .Concat(cardsE.Cards);
            var inxes = allCardLists.Select(c => c.CardInx).ToList();
            var distinct = inxes.Distinct().ToList();

            Assert.That(distinct.Count, Is.EqualTo(inxes.Count), "Every CardInx should be unique across the deck");
        });
    }

    [Test]
    public async Task ShuffleRetainsAllCardInxes()
    {
        await Pair.CreateTestMap();
        var coords = Pair.TestMap!.GridCoords;

        await Server.WaitAssertion(() =>
        {
            var (uid, cards, _) = SpawnDeck(coords);
            var before = cards.Cards.Select(c => c.CardInx).OrderBy(x => x).ToList();

            _sCards.TryShuffleCards((uid, cards));

            var after = cards.Cards.Select(c => c.CardInx).OrderBy(x => x).ToList();
            Assert.That(after, Is.EqualTo(before), "After shuffle the same CardInxes must all be present");
        });
    }

    [Test]
    public async Task CardsPrototypeCheck()
    {
        await Server.WaitAssertion(() =>
        {
            var protoIds = Pair.GetPrototypesWithComponent<CardsComponent>();

            foreach (var (proto, cardsComp) in protoIds)
            {
                if (proto.HideSpawnMenu)
                    continue;
                if (!proto.TryGetComponent<StackComponent>(out var stack, _sCompFact))
                    Assert.Fail($"prototype: {proto.ID} requires a {nameof(StackComponent)}");

                Assert.That(cardsComp.Cards.Count, Is.EqualTo(stack.Count));

                var stackType = stack.StackTypeId;
                if (!SProtoMan.TryIndex(stackType, out var stackProto))
                    Assert.Fail();

                if (!SProtoMan.TryIndex(stackProto.Spawn, out var baseCard))
                    Assert.Fail();

                if (!baseCard.TryGetComponent<CardsComponent>(out var baseCardComp, _sCompFact))
                    Assert.Fail(
                        $"{baseCard.ID} the spawn of {stackType} which is the stack of {proto.ID} requires a {nameof(StackComponent)}"
                    );

                if (!baseCard.TryGetComponent<StackComponent>(out var baseStackComp, _sCompFact))
                    Assert.Fail(
                        $"{baseCard.ID} the spawn of {stackType} which is the stack of {proto.ID} requires a {nameof(StackComponent)}"
                    );

                Assert.That(baseCardComp.Cards.Count, Is.EqualTo(0));
                Assert.That(baseStackComp.Count, Is.EqualTo(1));
                Assert.That(baseStackComp.StackTypeId, Is.EqualTo(stack.StackTypeId));
            }
        });
    }
}
