#nullable enable
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
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
    private static readonly EntProtoId ChasmProto = "FloorChasmEntity";
    private static readonly EntProtoId CatWalkProto = "Catwalk";
    private static readonly EntProtoId GrapplingGunProto = "WeaponGrapplingGun";

    [SidedDependency(Side.Server)] private SharedGrapplingGunSystem _sGrapplingSystem = default!;

    [Test]
    [Description("Tests that a player falls into a chasm when walking over it.")]
    public async Task ChasmFallTest()
    {
        // Spawn a chasm.
        await SpawnTarget(ChasmProto);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not spawn left of the chasm.");

        // Attempt (and fail) to walk past the chasm.
        // If you are modifying the default value of ChasmFallingComponent.DeletionTime this time might need to be adjusted.
        await Move(DirectionFlag.East, 0.5f);

        // We should be falling right now.
        Assert.That(TryComp<ChasmFallingComponent>(Player, out var falling), "Player is not falling after walking over a chasm.");

        var fallTime = (float)falling!.DeletionTime.TotalSeconds;

        // Wait until we get deleted.
        await RunSeconds(fallTime);

        // Check that the player was deleted.
        AssertDeleted(Player);
    }

    [Test]
    [Description("Test that a catwalk placed over a chasm will protect a player from falling.")]
    public async Task ChasmCatwalkTest()
    {
        // Spawn a chasm.
        await SpawnTarget(ChasmProto);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not spawn left of the chasm.");

        // Spawn a catwalk over the chasm.
        var catwalk = await Spawn(CatWalkProto);

        // Attempt to walk past the chasm.
        await Move(DirectionFlag.East, 1f);

        // We should be on the other side.
        Assert.That(Delta(), Is.LessThan(-0.5), "Player was unable to walk over a chasm with a catwalk.");

        // Check that the player is not deleted.
        AssertExists(Player);

        // Make sure the player is not falling right now.
        Assert.That(SPlayer, Has.No.Comp<ChasmFallingComponent>(Server), $"Player has {nameof(ChasmFallingComponent)} after walking over a catwalk.");

        // Delete the catwalk.
        await Delete(catwalk);

        // Attempt (and fail) to walk past the chasm.
        await Move(DirectionFlag.West, 1f);

        // Wait until we get deleted.
        await RunSeconds(5f);

        // Check that the player was deleted
        AssertDeleted(Player);
    }

    [Test]
    [Description("Tests that a player is able to cross a chasm by using a grappling gun.")]
    public async Task ChasmGrappleTest()
    {
        // Spawn a chasm.
        await SpawnTarget(ChasmProto);
        Assert.That(Delta(), Is.GreaterThan(0.5), "Player did not spawn left of the chasm.");

        // Give the player a grappling gun.
        var grapplingGun = await PlaceInHands(GrapplingGunProto);
        await Pair.RunSeconds(2f); // guns have a cooldown when picking them up

        // Shoot at the wall to the right.
        Assert.That(WallRight, Is.Not.Null, "No wall to shoot at!");
        await AttemptShoot(WallRight);
        await RunSeconds(2f);

        // Check that the grappling hook is embedded into the wall.
        Assert.That(TryComp<GrapplingGunComponent>(grapplingGun, out var grapplingGunComp), $"Grappling gun did not have {nameof(GrapplingGunComponent)}.");
        Assert.That(grapplingGunComp?.Projectile, Is.Not.Null, "Grappling gun projectile does not exist.");
        Assert.That(STryComp<EmbeddableProjectileComponent>(grapplingGunComp.Projectile, out var embeddable), "Grappling hook was not embeddable.");
        Assert.That(embeddable?.EmbeddedIntoUid, Is.EqualTo(ToServer(WallRight)), "Grappling hook was not embedded into the wall.");

        // Check that the player is hooked.
        Assert.That(_sGrapplingSystem.IsEntityHooked(SPlayer), "Player is not hooked to the wall.");
        Assert.That(SPlayer, Has.Comp<JointRelayTargetComponent>(Server), $"Player does not have the {nameof(JointRelayTargetComponent)} after using a grappling gun.");

        // Attempt to walk past the chasm.
        await Move(DirectionFlag.East, 1f);

        // We should be on the other side.
        Assert.That(Delta(), Is.LessThan(-0.5), "Player was unable to walk over a chasm with a grappling gun.");

        // Check that the player is not deleted.
        AssertExists(Player);

        // Make sure the player is not falling right now.
        Assert.That(SPlayer, Has.No.Comp<ChasmFallingComponent>(Server), $"Player has {nameof(ChasmFallingComponent)} after moving over a chasm with a grappling gun.");

        // Drop the grappling gun.
        await Drop();

        // Check that the player no longer hooked.
        Assert.That(_sGrapplingSystem.IsEntityHooked(SPlayer), Is.False, "Player still hooked after dropping the grappling gun.");
        Assert.That(SPlayer, Has.No.Comp<JointRelayTargetComponent>(Server), $"Player still has the {nameof(JointRelayTargetComponent)} after dropping the grappling gun.");
    }
}
