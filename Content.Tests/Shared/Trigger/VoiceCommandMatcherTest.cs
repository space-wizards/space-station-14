using System.Collections.Generic;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Triggers;
using NUnit.Framework;

namespace Content.Tests.Shared.Trigger;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class VoiceCommandMatcherTest
{
    private static VoiceCommandsComponent Build(float threshold = 0.5f, params (string phrase, string tag)[] triggers)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (p, t) in triggers)
            dict[p] = t;

        return new VoiceCommandsComponent
        {
            Triggers = dict,
            MatchThreshold = threshold,
            Candidates = VoiceCommandMatcher.BuildVoiceCommandCandidates(dict),
        };
    }

    private static readonly Dictionary<string, int> NoSpokenDigits = [];

    private static void AssertMatch(VoiceCommandsComponent comp, string spoken, string tag, int quantity = 1)
    {
        Assert.That(VoiceCommandMatcher.TryMatchVoiceCommand(comp, spoken, NoSpokenDigits, out var matchedTag, out var matchedQty), Is.True, $"\"{spoken}\" did not match");
        Assert.That(matchedTag, Is.EqualTo(tag), $"\"{spoken}\" tag");
        Assert.That(matchedQty, Is.EqualTo(quantity), $"\"{spoken}\" quantity");
    }

    private static void AssertNoMatch(VoiceCommandsComponent comp, string spoken)
    {
        Assert.That(VoiceCommandMatcher.TryMatchVoiceCommand(comp, spoken, NoSpokenDigits, out _, out _), Is.False, $"\"{spoken}\" unexpectedly matched");
    }

    [Test]
    public void SurfaceNoiseStillMatches()
    {
        var comp = Build(triggers: ("popcorn", "FoodPopcorn"));
        Assert.Multiple(() =>
        {
            AssertMatch(comp, "popcorn", "FoodPopcorn");
            AssertMatch(comp, "POPCORN", "FoodPopcorn");
            AssertMatch(comp, "popcorn!", "FoodPopcorn");
            AssertMatch(comp, "can I get some popcorn please", "FoodPopcorn");
        });
    }

    [Test]
    public void QuantityExtraction()
    {
        var comp = Build(triggers: ("popcorn", "FoodPopcorn"));
        Assert.Multiple(() =>
        {
            AssertMatch(comp, "5 popcorn", "FoodPopcorn", 5);
            AssertMatch(comp, "500 popcorn", "FoodPopcorn", 500); // big counts go through digits
            AssertMatch(comp, "2 popcorn 5", "FoodPopcorn", 2);
            AssertNoMatch(comp, "-3 popcorn");
        });
    }

    [Test]
    public void NumberInItemNameIsNotEatenAsQuantity()
    {
        // "B-52" orders one B-52, not 52 "b"'s.
        var comp = Build(triggers: ("b 52", "DrinkB52"));
        Assert.Multiple(() =>
        {
            AssertMatch(comp, "b 52", "DrinkB52", 1);
            AssertMatch(comp, "5 b 52", "DrinkB52", 5);
        });
    }

    [Test]
    public void RejectsNonCommands()
    {
        var comp = Build(triggers: ("popcorn", "FoodPopcorn"));
        AssertNoMatch(comp, "   ");
        AssertNoMatch(comp, "5"); // a bare count with no item
    }

    [Test]
    public void NoTriggersNeverMatches()
    {
        AssertNoMatch(Build(), "popcorn");
    }

    [Test]
    public void RanksMostSpecificTrigger()
    {
        var comp = Build(triggers: new[] { ("apple", "FoodApple"), ("apple juice", "DrinkAppleJuice") });
        Assert.Multiple(() =>
        {
            AssertMatch(comp, "apple juice", "DrinkAppleJuice");
            AssertMatch(comp, "apple", "FoodApple");
        });
    }

    [Test]
    public void MatchesOnPartialOverlap()
    {
        Assert.Multiple(() =>
        {
            // Spoken phrase contains more of the longer trigger.
            var fried = Build(triggers: new[] { ("chicken", "FoodChicken"), ("southern fried chicken", "FoodFriedChicken") });
            AssertMatch(fried, "fried chicken", "FoodFriedChicken");

            // Spoken phrase fits inside a longer trigger.
            var pizza = Build(triggers: new[] { ("arnolds pizza", "FoodPizzaArnold"), ("popcorn", "FoodPopcorn") });
            AssertMatch(pizza, "pizza", "FoodPizzaArnold");

            // Plural of a word inside a multi-word trigger.
            var beer = Build(triggers: ("beer can", "DrinkBeerCan"));
            AssertMatch(beer, "beers", "DrinkBeerCan");
        });
    }

    [Test]
    public void ThresholdGating()
    {
        Assert.Multiple(() =>
        {
            // "acorn" overlaps "popcorn" but well below 0.9.
            AssertNoMatch(Build(threshold: 0.9f, triggers: ("popcorn", "FoodPopcorn")), "acorn");

            // Threshold > 1 is clamped; an exact (1.0) match still passes.
            AssertMatch(Build(threshold: 5f, triggers: ("popcorn", "FoodPopcorn")), "popcorn", "FoodPopcorn");

            // Threshold 0 must not become a catch-all: unrelated speech still fails.
            AssertNoMatch(Build(threshold: 0f, triggers: ("popcorn", "FoodPopcorn")), "supercalifragilisticexpialidocious");
        });
    }

    [Test]
    public void PaddingKeepsShortTriggersDistinct()
    {
        // Unpadded, "s"'s trigrams would be a subset of "screwdriver" and match it.
        AssertNoMatch(Build(triggers: ("s", "ItemS")), "screwdriver");
    }

    [Test]
    public void FuzzyKeyphraseExtraction()
    {
        Assert.Multiple(() =>
        {
            Assert.That(VoiceCommandMatcher.TryExtractKeyphrase("autolathe, ten crowbars", "autolathe", false, 0.5f, out var exact), Is.True);
            Assert.That(exact, Is.EqualTo("ten crowbars"));

            // OwO accent works.
            Assert.That(VoiceCommandMatcher.TryExtractKeyphrase("computew popcown", "computer", true, 0.5f, out var owo), Is.True);
            Assert.That(owo, Is.EqualTo("popcown"));

            // Stutter works.
            Assert.That(VoiceCommandMatcher.TryExtractKeyphrase("c-c-omputer popcorn", "computer", true, 0.5f, out var stutter), Is.True);
            Assert.That(stutter, Does.Contain("popcorn"));
        });
    }
}
