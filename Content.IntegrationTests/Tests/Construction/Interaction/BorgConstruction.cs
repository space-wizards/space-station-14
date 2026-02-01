using Content.IntegrationTests.Tests.Interaction;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

[TestFixture]
public sealed class BorgConstruction : InteractionTest
{
    /// <summary>
    /// ID of the base endoskeleton to which parts will be added.
    /// </summary>
    private static readonly EntProtoId BorgEndoskeletonId = "CyborgEndoskeleton";

    /// <summary>
    /// ID of the entity that should be produced when construction is complete.
    /// </summary>
    private static readonly EntProtoId BorgCompleteId = "BorgChassisSelectable";

    /// <summary>
    /// ID of the Flashes needed for borg construction.
    /// </summary>
    private static readonly EntProtoId FlashId = "Flash";

    /// <summary>
    /// IDs of all the parts that need to be added to the endoskeleton to complete construction.
    /// </summary>
    private static readonly EntProtoId[] Parts =
    [
        "TorsoBorg",
        "LightHeadBorg",
        "LeftArmBorg",
        "RightArmBorg",
        "LeftLegBorg",
        "RightLegBorg",
    ];

    /// <summary>
    /// Spawns an endoskeleton and makes the player add each part to it by interacting with the parts in hand.
    /// Verifies that the correct entity is produced as a result.
    /// </summary>
    [Test]
    public async Task ConstructBorg()
    {
        // Spawn the endoskeleton frame.
        await SpawnTarget(BorgEndoskeletonId);

        // Have the player add each part needed for construction.
        foreach (var part in Parts)
        {
            await InteractUsing(part);
        }

        // Finish construction.
        await Interact(
            Cable,
            FlashId,
            FlashId,
            Screw
        );

        // Construction should have finished - make sure to the resulting entity is correct.
        AssertPrototype(BorgCompleteId, Target);
    }
}
