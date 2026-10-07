using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Weapons;

/// <summary>
/// Checks that when the shooter's client predicts firing a magazine-fed gun, the round it chambers from the magazine is
/// a predicted spawn, so the engine replaces it with the server's round once the server's state arrives.
/// </summary>
public sealed class MagazinePredictionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId Pistol = "WeaponPistolMk58";

    [Test]
    public async Task PredictedChamberedRoundIsPredictedSpawn()
    {
        await AddAtmosphere(); // prevent the Urist from suffocating

        var gun = await PlaceInHands(Pistol);
        await UseInHand(); // Chamber the gun
        await Pair.RunSeconds(2f); // Guns have a cooldown when picking them up.
        await SetCombatMode(true);

        // The client has to know it's in combat mode too, or it won't predict the shot.
        for (var i = 0; i < 20 && !CEntMan.GetComponent<CombatModeComponent>(CPlayer).IsInCombatMode; i++)
        {
            await RunTicks(1);
        }

        Assert.That(CEntMan.GetComponent<CombatModeComponent>(CPlayer).IsInCombatMode,
            "The client never entered combat mode, so nothing was predicted.");

        var aimCoords = SEntMan.GetNetCoordinates(SEntMan.GetCoordinates(TargetCoords).Offset(new Vector2(10f, 0f)));

        await Client.WaitAssertion(() =>
        {
            CEntMan.RaisePredictiveEvent(new RequestShootEvent
            {
                Gun = gun,
                Coordinates = aimCoords,
            });

            // The fresh magazine has no round entities yet, so the client spawns the next round itself.
            var chamber = GetClientChamber(gun);
            Assert.That(chamber.ContainedEntities, Has.Count.EqualTo(1), "The client didn't chamber the next round.");

            var round = chamber.ContainedEntities[0];
            Assert.That(!CEntMan.IsClientSide(round) || CEntMan.HasComponent<PredictedSpawnComponent>(round),
                "The client put a client-side round in the chamber that isn't a predicted spawn.");
        });

        // Plenty of time for the server's state to arrive.
        await RunTicks(10);

        await Client.WaitAssertion(() =>
        {
            var chamber = GetClientChamber(gun);
            Assert.That(chamber.ContainedEntities, Has.Count.EqualTo(1), "The next round wasn't chambered.");
            Assert.That(CEntMan.IsClientSide(chamber.ContainedEntities[0]),
                Is.False,
                "The client's predicted round is still in the chamber.");
        });
    }

    private BaseContainer GetClientChamber(NetEntity gun)
    {
        var containers = CEntMan.System<SharedContainerSystem>();
        Assert.That(containers.TryGetContainer(CEntMan.GetEntity(gun), SharedGunSystem.ChamberSlot, out var chamber));
        return chamber!;
    }
}
