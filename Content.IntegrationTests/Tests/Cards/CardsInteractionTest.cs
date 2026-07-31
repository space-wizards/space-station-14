using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Cards;

namespace Content.IntegrationTests.Cards;

[TestFixture]
public sealed class CardsInteractionTest : InteractionTest
{
    private const string CardsProtoId = "CardDeck";

    [Test]
    public async Task FlipCycleOnUseInHand()
    {
        var cards = await SpawnTarget(CardsProtoId);

        if (!SEntMan.TryGetComponent<CardsComponent>(SEntMan.GetEntity(cards), out var sCardsComp))
            Assert.Fail($"Missing {nameof(CardsComponent)}");

        await Pickup();

        await UseInHand();
        Assert.That(sCardsComp!.Flipped, Is.True);
        Assert.That(sCardsComp.Fanned, Is.False);

        await UseInHand();
        Assert.That(sCardsComp.Flipped, Is.True);
        Assert.That(sCardsComp.Fanned, Is.True);

        await UseInHand();
        Assert.That(sCardsComp.Fanned, Is.False);
        Assert.That(sCardsComp.Flipped, Is.False);
    }

    [Test]
    public async Task FlipOnActivate()
    {
        var cards = await SpawnTarget(CardsProtoId);

        if (!SEntMan.TryGetComponent<CardsComponent>(SEntMan.GetEntity(cards), out var sCardsComp))
            Assert.Fail($"Missing {nameof(CardsComponent)}");

        Assert.That(sCardsComp!.Flipped, Is.False);

        await Activate();
        Assert.That(sCardsComp!.Flipped, Is.True);

        await Activate();
        Assert.That(sCardsComp!.Flipped, Is.False);
    }

    [Test]
    public async Task ThrowReducesCardCount()
    {
        var cards = await SpawnTarget(CardsProtoId);

        if (!SEntMan.TryGetComponent<CardsComponent>(SEntMan.GetEntity(cards), out var sCardsComp))
            Assert.Fail("Missing CardsComponent");

        var cardCountBefore = sCardsComp!.Cards.Count;

        await Pickup();
        // Flip so cards are face-up (needed for the throw-one-card behaviour)
        await UseInHand();

        var coords = Transform.GetMapCoordinates(CPlayer).Offset(new Vector2(0, 10));
        await ThrowItem(SEntMan.GetNetCoordinates(Transform.ToCoordinates(CPlayer, coords)));

        Assert.That(
            sCardsComp.Cards.Count,
            Is.EqualTo(cardCountBefore - 1),
            "Throwing one card should decrease the deck by 1"
        );
    }

    [Test]
    public async Task CardsInteraction()
    {
        var cards = await SpawnTarget(CardsProtoId);

        if (!SEntMan.TryGetComponent<CardsComponent>(SEntMan.GetEntity(cards), out var sCardsComp))
            Assert.Fail($"Missing {nameof(CardsComponent)}");

        var cardCountBefore = sCardsComp!.Cards.Count;

        await Pickup();

        // Flip the deck
        await UseInHand();
        Assert.That(sCardsComp.Flipped, Is.EqualTo(true));

        // Fan the deck
        await UseInHand();
        Assert.That(sCardsComp.Flipped, Is.EqualTo(true));
        Assert.That(sCardsComp.Fanned, Is.EqualTo(true));

        // Throw one card
        var coords = Transform.GetMapCoordinates(CPlayer).Offset(new Vector2(0, 10));
        await ThrowItem(SEntMan.GetNetCoordinates(Transform.ToCoordinates(CPlayer, coords)));

        Assert.That(sCardsComp.Cards.Count, Is.EqualTo(cardCountBefore - 1));
        Assert.That(sCardsComp.Flipped, Is.EqualTo(true));
        Assert.That(sCardsComp.Fanned, Is.EqualTo(true));
    }
}
