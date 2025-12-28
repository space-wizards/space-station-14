using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Tests for asserting that various gas specific heat operations agree with each other and do not deviate
/// across client and server.
/// </summary>
[TestFixture]
[TestOf(nameof(SharedAtmosphereSystem))]
public sealed class SharedGasSpecificHeatsTest : AtmosTest
{
    // nullref if no map is present
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    private IConfigurationManager _sConfig;
    private IConfigurationManager _cConfig;

    /// <summary>
    /// Asserts that the cached gas specific heat arrays agree with each other.
    /// </summary>
    [Test]
    public async Task GasSpecificHeats_Agree()
    {
        var serverSpecificHeats = Array.Empty<float>();
        var clientSpecificHeats = Array.Empty<float>();
        await Server.WaitPost(delegate
        {
            serverSpecificHeats = SAtmos.GasSpecificHeats;
        });

        await Client.WaitPost(delegate
        {
            clientSpecificHeats = CAtmos.GasSpecificHeats;
        });

        Assert.That(serverSpecificHeats, Is.EqualTo(clientSpecificHeats));
    }

    /// <summary>
    /// Asserts that heat capacity calculations agree for the same gas mixture.
    /// </summary>
    [Test]
    public async Task HeatCapacity_Agree()
    {
        const float volume = 2500f;
        const float temperature = 293.15f;

        const float o2 = 12.3f;
        const float n2 = 45.6f;
        const float co2 = 0.42f;
        const float plasma = 0.05f;

        var serverScaled = 0f;
        var serverUnscaled = 0f;
        var clientScaled = 0f;
        var clientUnscaled = 0f;

        await Server.WaitPost(delegate
        {
            var mix = new GasMixture(volume) { Temperature = temperature };
            mix.AdjustMoles(Gas.Oxygen, o2);
            mix.AdjustMoles(Gas.Nitrogen, n2);
            mix.AdjustMoles(Gas.CarbonDioxide, co2);
            mix.AdjustMoles(Gas.Plasma, plasma);

            serverScaled = SAtmos.GetHeatCapacity(mix, applyScaling: true);
            serverUnscaled = SAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        await Client.WaitPost(delegate
        {
            var mix = new GasMixture(volume) { Temperature = temperature };
            mix.AdjustMoles(Gas.Oxygen, o2);
            mix.AdjustMoles(Gas.Nitrogen, n2);
            mix.AdjustMoles(Gas.CarbonDioxide, co2);
            mix.AdjustMoles(Gas.Plasma, plasma);

            clientScaled = CAtmos.GetHeatCapacity(mix, applyScaling: true);
            clientUnscaled = CAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        // none of these should be exploding or nonzero.
        // they could potentially agree at insane values and pass the test
        // so check for if they're sane.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverScaled, Is.GreaterThan(0f));
            Assert.That(serverUnscaled, Is.GreaterThan(0f));
            Assert.That(clientScaled, Is.GreaterThan(0f));
            Assert.That(clientUnscaled, Is.GreaterThan(0f));

            Assert.That(float.IsFinite(serverScaled), Is.True);
            Assert.That(float.IsFinite(serverUnscaled), Is.True);
            Assert.That(float.IsFinite(clientScaled), Is.True);
            Assert.That(float.IsFinite(clientUnscaled), Is.True);
        }

        const float epsilon = 1e-4f;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverScaled, Is.EqualTo(clientScaled).Within(epsilon));
            Assert.That(serverUnscaled, Is.EqualTo(clientUnscaled).Within(epsilon));

            Assert.That(serverUnscaled, Is.EqualTo(serverScaled * SAtmos.HeatScale).Within(epsilon));
            Assert.That(clientUnscaled, Is.EqualTo(clientScaled * CAtmos.HeatScale).Within(epsilon));
        }
    }

    /// <summary>
    /// HeatScale CVAR is required for specific heat calculations.
    /// Assert that they agree across client and server, and that changing the CVAR
    /// replicates properly and updates the cached value.
    /// Also assert that calculations using the updated HeatScale agree properly.
    /// </summary>
    [Test]
    public async Task HeatScaleCVar_ReplicatesAndAgree()
    {
        // ensure that replicated value changes by testing a new value
        const float newHeatScale = 13f;

        _sConfig = Server.ResolveDependency<IConfigurationManager>();
        _cConfig = Client.ResolveDependency<IConfigurationManager>();

        await Server.WaitPost(delegate
        {
            _sConfig.SetCVar(CCVars.AtmosHeatScale, newHeatScale);
        });

        await Server.WaitRunTicks(5);
        await Client.WaitRunTicks(5);

        // assert agreement between client and server
        float serverCVar = 0;
        float clientCVar = 0;
        float serverHeatScale = 0;
        float clientHeatScale = 0;

        await Server.WaitPost(delegate
        {
            serverCVar = _sConfig.GetCVar(CCVars.AtmosHeatScale);
            serverHeatScale = SAtmos.HeatScale;
        });

        await Client.WaitPost(delegate
        {
            clientCVar = _cConfig.GetCVar(CCVars.AtmosHeatScale);
            clientHeatScale = CAtmos.HeatScale;
        });

        const float epsilon = 1e-4f;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverCVar, Is.EqualTo(newHeatScale).Within(epsilon));
            Assert.That(clientCVar, Is.EqualTo(newHeatScale).Within(epsilon));

            Assert.That(serverHeatScale, Is.EqualTo(newHeatScale).Within(epsilon));
            Assert.That(clientHeatScale, Is.EqualTo(newHeatScale).Within(epsilon));

            Assert.That(serverHeatScale, Is.EqualTo(clientHeatScale).Within(epsilon));
        }

        // verify that anything calculated using the shared HeatScale agrees properly
        const float volume = 2500f;
        const float temperature = 293.15f;

        var sScaled = 0f;
        var sUnscaled = 0f;
        var cScaled = 0f;
        var cUnscaled = 0f;

        await Server.WaitPost(delegate
        {
            var mix = new GasMixture(volume) { Temperature = temperature };
            mix.AdjustMoles(Gas.Oxygen, 10f);
            mix.AdjustMoles(Gas.Nitrogen, 20f);

            sScaled = SAtmos.GetHeatCapacity(mix, applyScaling: true);
            sUnscaled = SAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        await Client.WaitPost(delegate
        {
            var mix = new GasMixture(volume) { Temperature = temperature };
            mix.AdjustMoles(Gas.Oxygen, 10f);
            mix.AdjustMoles(Gas.Nitrogen, 20f);

            cScaled = CAtmos.GetHeatCapacity(mix, applyScaling: true);
            cUnscaled = CAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sScaled, Is.GreaterThan(0f));
            Assert.That(cScaled, Is.GreaterThan(0f));

            Assert.That(sUnscaled, Is.EqualTo(sScaled * serverHeatScale).Within(epsilon));
            Assert.That(cUnscaled, Is.EqualTo(cScaled * clientHeatScale).Within(epsilon));
        }
    }
}
