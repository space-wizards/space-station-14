using Content.Server.Atmos.Piping.Trinary.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Test for ensuring that atmospherics components deserialize and spawn with settings applied.
/// </summary>
[TestFixture]
public sealed class AtmosMapLoadSettingsTest : AtmosTest
{
    // These values correspond to settings in the test map.
    private const float PumpSetting = 180f;
    private const float MixerMainSetting = 0.1f;
    private const float MixerSideSetting = 0.9f;

    /// <summary>
    /// Test map containing saved atmos components.
    /// </summary>
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/load_atmos_test_room.yml");

    /// <summary>
    /// Test to verify that the settings have been properly applied.
    /// </summary>
    [Test]
    public async Task TestMapLoading()
    {
        await Server.WaitAssertion(() =>
        {
            var volumePumpQuery = SEntMan.EntityQueryEnumerator<GasVolumePumpComponent>();
            while (volumePumpQuery.MoveNext(out var volumePump))
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(volumePump.Enabled, Is.True, "Volume pump did not load enabled!");
                    Assert.That(
                        volumePump.TransferRate,
                        Is.EqualTo(PumpSetting),
                        "Volume pump did not load correct setting!"
                        );
                }
            }

            var pressurePumpQuery = SEntMan.EntityQueryEnumerator<GasPressurePumpComponent>();
            while (pressurePumpQuery.MoveNext(out var pressurePump))
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(pressurePump.Enabled, Is.True, "Pressure pump did not load enabled!");
                    Assert.That(
                        pressurePump.TargetPressure,
                        Is.EqualTo(PumpSetting),
                        "Pressure pump did not load correct setting!"
                        );
                }
            }

            var mixerQuery = SEntMan.EntityQueryEnumerator<GasMixerComponent>();
            while (mixerQuery.MoveNext(out var mixer))
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(mixer.Enabled, Is.True, "Mixer did not load enabled!");
                    Assert.That(
                        mixer.TargetPressure,
                        Is.EqualTo(PumpSetting),
                        "Mixer pump did not load correct setting!"
                        );
                    Assert.That(
                        mixer.InletOneConcentration,
                        Is.EqualTo(MixerMainSetting),
                        "Mixer split did not load correct setting!"
                        );
                    Assert.That(
                        mixer.InletTwoConcentration,
                        Is.EqualTo(MixerSideSetting),
                        "Mixer split did not load correct setting!"
                        );
                }
            }

        });
    }
}
