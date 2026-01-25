using Content.Client.Atmos.EntitySystems;
using Content.IntegrationTests.Pair;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Tests for asserting that various gas specific heat operations agree with each other and do not deviate
/// across client and server.
/// </summary>
[TestOf(nameof(SharedAtmosphereSystem))]
public sealed class SharedGasSpecificHeatsTest
{
    private IConfigurationManager _sConfig;
    private IConfigurationManager _cConfig;

    private TestPair _pair = default!;

    private RobustIntegrationTest.ServerIntegrationInstance Server => _pair.Server;
    private RobustIntegrationTest.ClientIntegrationInstance Client => _pair.Client;

    private IEntityManager _sEntMan = default!;
    private Content.Server.Atmos.EntitySystems.AtmosphereSystem _sAtmos = default!;

    private IEntityManager _cEntMan = default!;
    private AtmosphereSystem _cAtmos = default!;

    [SetUp]
    public async Task SetUp()
    {
        var poolSettings = new PoolSettings
        {
            Connected = true,
        };
        _pair = await PoolManager.GetServerClient(poolSettings);

        _sEntMan = Server.ResolveDependency<IEntityManager>();
        _cEntMan = Client.ResolveDependency<IEntityManager>();

        _sAtmos = _sEntMan.System<Content.Server.Atmos.EntitySystems.AtmosphereSystem>();
        _cAtmos = _cEntMan.System<AtmosphereSystem>();
    }

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
            serverSpecificHeats = _sAtmos.GasSpecificHeats;
        });

        await Client.WaitPost(delegate
        {
            clientSpecificHeats = _cAtmos.GasSpecificHeats;
        });

        Assert.That(serverSpecificHeats,
            Is.EqualTo(clientSpecificHeats),
            "Server and client gas specific heat arrays do not agree.");
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

            serverScaled = _sAtmos.GetHeatCapacity(mix, applyScaling: true);
            serverUnscaled = _sAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        await Client.WaitPost(delegate
        {
            var mix = new GasMixture(volume) { Temperature = temperature };
            mix.AdjustMoles(Gas.Oxygen, o2);
            mix.AdjustMoles(Gas.Nitrogen, n2);
            mix.AdjustMoles(Gas.CarbonDioxide, co2);
            mix.AdjustMoles(Gas.Plasma, plasma);

            clientScaled = _cAtmos.GetHeatCapacity(mix, applyScaling: true);
            clientUnscaled = _cAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        // none of these should be exploding or nonzero.
        // they could potentially agree at insane values and pass the test
        // so check for if they're sane.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverScaled,
                Is.GreaterThan(0f),
                "Heat capacity calculated on server with scaling is not greater than zero.");
            Assert.That(serverUnscaled,
                Is.GreaterThan(0f),
                "Heat capacity calculated on server without scaling is not greater than zero.");
            Assert.That(clientScaled,
                Is.GreaterThan(0f),
                "Heat capacity calculated on client with scaling is not greater than zero.");
            Assert.That(clientUnscaled,
                Is.GreaterThan(0f),
                "Heat capacity calculated on client without scaling is not greater than zero.");

            Assert.That(float.IsFinite(serverScaled),
                Is.True,
                "Heat capacity calculated on server with scaling is not finite.");
            Assert.That(float.IsFinite(serverUnscaled),
                Is.True,
                "Heat capacity calculated on server without scaling is not finite.");
            Assert.That(float.IsFinite(clientScaled),
                Is.True,
                "Heat capacity calculated on client with scaling is not finite.");
            Assert.That(float.IsFinite(clientUnscaled),
                Is.True,
                "Heat capacity calculated on client without scaling is not finite.");
        }

        const float epsilon = 1e-4f;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverScaled,
                Is.EqualTo(clientScaled).Within(epsilon),
                "Heat capacity calculated with scaling does not agree between client and server.");
            Assert.That(serverUnscaled,
                Is.EqualTo(clientUnscaled).Within(epsilon),
                "Heat capacity calculated without scaling does not agree between client and server.");

            Assert.That(serverUnscaled,
                Is.EqualTo(serverScaled * _sAtmos.HeatScale).Within(epsilon),
                "Heat capacity calculated on server without scaling does not equal scaled value multiplied by HeatScale.");
            Assert.That(clientUnscaled,
                Is.EqualTo(clientScaled * _cAtmos.HeatScale).Within(epsilon),
                "Heat capacity calculated on client without scaling does not equal scaled value multiplied by HeatScale.");
        }
    }

    /// <summary>
    /// HeatScale CVAR is required for specific heat calculations.
    /// Assert that they agree across client and server, and that changing the CVAR
    /// replicates properly and updates the cached value.
    /// Also assert that calculations using the updated HeatScale agree properly.
    /// </summary>
    [Test]
    public async Task HeatScaleCVar_Replicates_Agree()
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
            serverHeatScale = _sAtmos.HeatScale;
        });

        await Client.WaitPost(delegate
        {
            clientCVar = _cConfig.GetCVar(CCVars.AtmosHeatScale);
            clientHeatScale = _cAtmos.HeatScale;
        });

        const float epsilon = 1e-4f;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(serverCVar,
                Is.EqualTo(newHeatScale).Within(epsilon),
                "Server CVAR value for AtmosHeatScale does not equal the set value.");
            Assert.That(clientCVar,
                Is.EqualTo(newHeatScale).Within(epsilon),
                "Client CVAR value for AtmosHeatScale does not equal the set value.");

            Assert.That(serverHeatScale,
                Is.EqualTo(newHeatScale).Within(epsilon),
                "Server cached HeatScale does not equal the set CVAR value.");
            Assert.That(clientHeatScale,
                Is.EqualTo(newHeatScale).Within(epsilon),
                "Client cached HeatScale does not equal the set CVAR value.");

            Assert.That(serverHeatScale,
                Is.EqualTo(clientHeatScale).Within(epsilon),
                "Client and server cached HeatScale values do not agree.");
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

            sScaled = _sAtmos.GetHeatCapacity(mix, applyScaling: true);
            sUnscaled = _sAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        await Client.WaitPost(delegate
        {
            var mix = new GasMixture(volume) { Temperature = temperature };
            mix.AdjustMoles(Gas.Oxygen, 10f);
            mix.AdjustMoles(Gas.Nitrogen, 20f);

            cScaled = _cAtmos.GetHeatCapacity(mix, applyScaling: true);
            cUnscaled = _cAtmos.GetHeatCapacity(mix, applyScaling: false);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sScaled,
                Is.GreaterThan(0f),
                "Heat capacity calculated on server with scaling is not greater than zero after CVAR change.");
            Assert.That(cScaled,
                Is.GreaterThan(0f),
                "Heat capacity calculated on client with scaling is not greater than zero after CVAR change.");

            Assert.That(sUnscaled,
                Is.EqualTo(sScaled * serverHeatScale).Within(epsilon),
                "Heat capacity calculated on server without scaling does not equal scaled value multiplied by updated HeatScale.");
            Assert.That(cUnscaled,
                Is.EqualTo(cScaled * clientHeatScale).Within(epsilon),
                "Heat capacity calculated on client without scaling does not equal scaled value multiplied by updated HeatScale.");
        }
    }
}
