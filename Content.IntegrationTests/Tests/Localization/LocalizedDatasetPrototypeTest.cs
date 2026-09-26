#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Dataset;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Localization;

public sealed class LocalizedDatasetPrototypeTest : GameTest
{
    [SidedDependency(Side.Server)] private ILocalizationManager _sLocalizationManager = null!;

    [TestOf(typeof(LocalizedDatasetPrototype))]
    [Description($"Checks that all LocIds in the {nameof(LocalizedDatasetPrototype)} are defined and that {nameof(LocalizedDatasetPrototype.Values.Count)} is correct.")]
    public async Task ValidLocIdsTest()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var proto in SProtoMan.EnumeratePrototypes<LocalizedDatasetPrototype>())
            {
                // Check each value in the prototype
                foreach (var locId in proto.Values)
                {
                    // Make sure the localization manager has a string for the LocId
                    Assert.That(_sLocalizationManager.HasString(locId), $"{nameof(LocalizedDatasetPrototype)} {proto.ID} with prefix \"{proto.Values.Prefix}\" specifies {proto.Values.Count} entries, but no localized string was found matching {locId}!");
                }

                // Check that count isn't set too low
                var nextId = proto.Values.Prefix + (proto.Values.Count + 1);
                Assert.That(_sLocalizationManager.HasString(nextId), Is.False, $"{nameof(LocalizedDatasetPrototype)} {proto.ID} with prefix \"{proto.Values.Prefix}\" specifies {proto.Values.Count} entries, but a localized string exists with ID {nextId}! Does count need to be raised?");
            }
        }
    }
}
