using Content.IntegrationTests.Tests.Movement;
using Content.Shared.Chasm;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Misc;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chasm;

/// <summary>
/// A test for chasms, which delete entities when a player walks over them.
/// </summary>
[TestOf(typeof(ChasmComponent))]
public sealed class ChasmTest : MovementTest
{
    private readonly EntProtoId _chasmProto = "FloorChasmEntity";
    private readonly EntProtoId _catWalkProto = "Catwalk";
    private readonly EntProtoId _grapplingGunProto = "WeaponGrapplingGun";

    /// <summary>
    /// Test that a player falls into the chasm when walking over it.
    /// </summary>
    [Test]
    public async Task ChasmFallTest()
    {
        // Spawn a chasm.
        await SpawnTarget(_chasmProto);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not spawn left of the chasm.");

        // Attempt (and fail) to walk past the chasm.
        // If you are modifying the default value of ChasmFallingComponent.DeletionTime this time might need to be adjusted.
        await Move(DirectionFlag.East, 0.5f);

        // We should be falling right now.
        Assert.That(TryComp<ChasmFallingComponent>(Player, out var falling), "Player is not falling after walking over a chasm.");

        var fallTime = (float)falling.DeletionTime.TotalSeconds;

        // Wait until we get deleted.
        await Pair.RunSeconds(fallTime);

        // Check that the player was deleted.
        AssertDeleted(Player);
    }

    /// <summary>
    /// Test that a catwalk placed over a chasm will protect a player from falling.
    /// </summary>
    [Test]
    public async Task ChasmCatwalkTest()
    {
        // Spawn a chasm.
        await SpawnTarget(_chasmProto);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not spawn left of the chasm.");

        // Spawn a catwalk over the chasm.
        var catwalk = await Spawn(_catWalkProto);

        // Attempt to walk past the chasm.
        await Move(DirectionFlag.East, 1f);

        // We should be on the other side.
        Assert.That(Delta(), Is.LessThan(-0.5), "Player was unable to walk over a chasm with a catwalk.");

        // Check that the player is not deleted.
        AssertExists(Player);

        // Make sure the player is not falling right now.
        Assert.That(HasComp<ChasmFallingComponent>(Player), Is.False, "Player has ChasmFallingComponent after walking over a catwalk.");

        // Delete the catwalk.
        await Delete(catwalk);

        // Attempt (and fail) to walk past the chasm.
        await Move(DirectionFlag.West, 1f);

        // Wait until we get deleted.
        await Pair.RunSeconds(5f);

        // Check that the player was deleted
        AssertDeleted(Player);
    }

    /// <summary>
    /// Tests that a player is able to cross a chasm by using a grappling gun.
    /// </summary>
    [Test]
    public async Task ChasmGrappleTest()
    {
        // Spawn a chasm.
        await SpawnTarget(_chasmProto);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not spawn left of the chasm.");

        // Give the player a grappling gun.
        var grapplingGun = await PlaceInHands(_grapplingGunProto);
        await Pair.RunSeconds(2f); // guns have a cooldown when picking them up

        // Shoot at the wall to the right.
        Assert.That(WallRight, Is.Not.Null, "No wall to shoot at!");
        await AttemptShoot(WallRight);
        await Pair.RunSeconds(2f);

        // Check that the grappling hook is embedded into the wall.
        Assert.That(TryComp<GrapplingGunComponent>(grapplingGun, out var grapplingGunComp), "Grappling gun did not have GrapplingGunComponent.");
        Assert.That(grapplingGunComp.Projectile, Is.Not.Null, "Grappling gun projectile does not exist.");
        Assert.That(SEntMan.TryGetComponent<EmbeddableProjectileComponent>(grapplingGunComp.Projectile, out var embeddable), "Grappling hook was not embeddable.");
        Assert.That(embeddable.EmbeddedIntoUid, Is.EqualTo(ToServer(WallRight)), "Grappling hook was not embedded into the wall.");

        // Check that the player is hooked.
        var grapplingSystem = SEntMan.System<SharedGrapplingGunSystem>();
        Assert.That(grapplingSystem.IsEntityHooked(SPlayer), "Player is not hooked to the wall.");
        Assert.That(HasComp<JointRelayTargetComponent>(Player), "Player does not have the JointRelayTargetComponent after using a grappling gun.");

        // Attempt to walk past the chasm.
        await Move(DirectionFlag.East, 1f);

        // We should be on the other side.
        Assert.That(Delta(), Is.LessThan(-0.5), "Player was unable to walk over a chasm with a grappling gun.");

        // Check that the player is not deleted.
        AssertExists(Player);

        // Make sure the player is not falling right now.
        Assert.That(HasComp<ChasmFallingComponent>(Player), Is.False, "Player has ChasmFallingComponent after moving over a chasm with a grappling gun.");

        // Drop the grappling gun.
        await Drop();

        // Check that the player no longer hooked.
        Assert.That(grapplingSystem.IsEntityHooked(SPlayer), Is.False, "Player still hooked after dropping the grappling gun.");
        Assert.That(HasComp<JointRelayTargetComponent>(Player), Is.False, "Player still has the JointRelayTargetComponent after dropping the grappling gun.");
    }
}
