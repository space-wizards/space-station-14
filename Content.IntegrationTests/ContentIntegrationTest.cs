using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Client.Entry;
using Content.Client.IoC;
using Content.Client.Parallax.Managers;
using Content.Server.GameTicking;
using Content.Server.IoC;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Client;
using Robust.Server;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests
{
    [Parallelizable(ParallelScope.All)]
    public abstract class ContentIntegrationTest : RobustIntegrationTest
    {
        protected sealed override ClientIntegrationInstance StartClient(ClientIntegrationOptions options = null)
        {
            options ??= new ClientContentIntegrationOption()
            {
                FailureLogLevel = LogLevel.Warning
            };

            options.Pool = ShouldPool(options);

            // Load content resources, but not config and user data.
            options.Options = new GameControllerOptions()
            {
                LoadContentResources = true,
                LoadConfigAndUserData = false,
            };

            options.ContentStart = true;

            options.ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(EntryPoint).Assembly,
                typeof(ContentIntegrationTest).Assembly
            };

            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks
                {
                    ClientBeforeIoC = () =>
                    {
                        if (options is ClientContentIntegrationOption contentOptions)
                        {
                            contentOptions.ContentBeforeIoC?.Invoke();
                        }

                        IoCManager.Register<IParallaxManager, DummyParallaxManager>(true);
                        IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
                    }
                });
            };

            return base.StartClient(options);
        }

        protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions options = null)
        {
            options ??= new ServerContentIntegrationOption
            {
                FailureLogLevel = LogLevel.Warning,
            };

            options.Pool = ShouldPool(options);

            // Load content resources, but not config and user data.
            options.Options = new ServerOptions()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = true,
            };

            options.ContentStart = true;

            options.ContentAssemblies = new[]
            {
                typeof(Shared.Entry.EntryPoint).Assembly,
                typeof(Server.Entry.EntryPoint).Assembly,
                typeof(ContentIntegrationTest).Assembly
            };

            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ServerModuleTestingCallbacks
                {
                    ServerBeforeIoC = () =>
                    {
                        if (options is ServerContentIntegrationOption contentOptions)
                        {
                            contentOptions.ContentBeforeIoC?.Invoke();
                        }
                    }
                });

                IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            };

            SetServerTestCvars(options.CVarOverrides);

            return base.StartServer(options);
        }

        protected ServerIntegrationInstance StartServerDummyTicker(ServerIntegrationOptions options = null)
        {
            options ??= new ServerContentIntegrationOption();

            // Load content resources, but not config and user data.
            options.Options = new ServerOptions()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = true,
            };

            // Dummy game ticker.
            options.CVarOverrides[CCVars.GameDummyTicker.Name] = "true";

            return StartServer(options);
        }

        protected async Task<(ClientIntegrationInstance client, ServerIntegrationInstance server)>
            StartConnectedServerClientPair(ClientIntegrationOptions clientOptions = null,
                ServerIntegrationOptions serverOptions = null)
        {
            serverOptions ??= new ServerIntegrationOptions {Pool = false};

            var client = StartClient(clientOptions);
            var server = StartServer(serverOptions);

            await StartConnectedPairShared(client, server);

            return (client, server);
        }

        protected async Task<(ClientIntegrationInstance client, ServerIntegrationInstance server)>
            StartConnectedServerDummyTickerClientPair(ClientIntegrationOptions clientOptions = null,
                ServerIntegrationOptions serverOptions = null)
        {
            var client = StartClient(clientOptions);
            var server = StartServerDummyTicker(serverOptions);

            await StartConnectedPairShared(client, server);

            return (client, server);
        }

        private void SetServerTestCvars(Dictionary<string, string> cvars)
        {
            // ShouldPool checks below need to match up with this

            // Avoid funny race conditions with the database.
            cvars[CCVars.DatabaseSynchronous.Name] = "true";

            // Disable holidays as some of them might mess with the map at round start.
            cvars[CCVars.HolidaysEnabled.Name] = "false";

            // Avoid loading a large map by default for integration tests if none has been specified.
            cvars.TryAdd(CCVars.GameMap.Name, "Maps/Test/empty.yml");
        }

        private bool ShouldPool(IntegrationOptions options)
        {
            if (!options.Pool)
            {
                return false;
            }

            if (options.CVarOverrides.Count != 3)
            {
                return false;
            }

            // Please save me from this method
            if (!options.CVarOverrides.TryGetValue(CCVars.DatabaseSynchronous.Name, out var syncDb) ||
                syncDb == "false")
            {
                return false;
            }

            if (!options.CVarOverrides.TryGetValue(CCVars.HolidaysEnabled.Name, out var holidaysEnabled) ||
                holidaysEnabled == "true")
            {
                return false;
            }

            if (!options.CVarOverrides.TryGetValue(CCVars.GameMap.Name, out var map) ||
                map != "Maps/Test/empty.yml")
            {
                return false;
            }

            if (options.CVarOverrides.TryGetValue(CCVars.GameDummyTicker.Name, out var dummy) &&
                dummy == "true")
            {
                return false;
            }

            if (options.CVarOverrides.TryGetValue(CCVars.GameLobbyEnabled.Name, out var lobby) &&
                lobby == "true")
            {
                return false;
            }

            if (options is ClientContentIntegrationOption {ContentBeforeIoC: { }}
                        or ServerContentIntegrationOption {ContentBeforeIoC: { }})
            {
                return false;
            }

            return options.InitIoC == null &&
                   options.BeforeStart == null &&
                   options.ContentAssemblies == null;
        }

        protected override async Task OnClientReturn(ClientIntegrationInstance client)
        {
            await base.OnClientReturn(client);

            await client.WaitIdleAsync();

            var net = client.ResolveDependency<IClientNetManager>();
            var prototypes = client.ResolveDependency<IPrototypeManager>();

            await client.WaitPost(() =>
            {
                net.ClientDisconnect("Test pooling disconnect");

                if (client.PreviousOptions?.ExtraPrototypes is { } oldExtra)
                {
                    prototypes.RemoveString(oldExtra);
                }

                if (client.Options?.ExtraPrototypes is { } extra)
                {
                    prototypes.LoadString(extra, true);
                    prototypes.Resync();
                }
            });

            await WaitUntil(client, () => !net.IsConnected);
        }

        protected override async Task OnServerReturn(ServerIntegrationInstance server)
        {
            await base.OnServerReturn(server);

            await server.WaitIdleAsync();

            if (server.Options != null)
            {
                SetServerTestCvars(server.Options.CVarOverrides);
            }

            var systems = server.ResolveDependency<IEntitySystemManager>();
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            var net = server.ResolveDependency<IServerNetManager>();

            var gameTicker = systems.GetEntitySystem<GameTicker>();

            await server.WaitPost(() =>
            {
                foreach (var channel in net.Channels)
                {
                    net.DisconnectChannel(channel, "Test pooling disconnect");
                }

                gameTicker.RestartRound();

                if (server.PreviousOptions?.ExtraPrototypes is { } oldExtra)
                {
                    prototypes.RemoveString(oldExtra);
                }

                if (server.Options?.ExtraPrototypes is { } extra)
                {
                    prototypes.LoadString(extra, true);
                    prototypes.Resync();
                }
            });

            if (!gameTicker.DummyTicker)
            {
                await WaitUntil(server, () => gameTicker.RunLevel == GameRunLevel.InRound, 600);
            }
        }

        protected async Task WaitUntil(IntegrationInstance instance, Func<bool> func, int maxTicks = 600,
            int tickStep = 1)
        {
            var ticksAwaited = 0;
            bool passed;

            await instance.WaitIdleAsync();

            while (!(passed = func()) && ticksAwaited < maxTicks)
            {
                var ticksToRun = tickStep;

                if (ticksAwaited + tickStep > maxTicks)
                {
                    ticksToRun = maxTicks - ticksAwaited;
                }

                await instance.WaitRunTicks(ticksToRun);

                ticksAwaited += ticksToRun;
            }

            if (!passed)
            {
                Assert.Fail($"Condition did not pass after {maxTicks} ticks. Tests ran: {string.Join('\n', instance.TestsRan)}");
            }
            Assert.That(passed);
        }

        private static async Task StartConnectedPairShared(ClientIntegrationInstance client,
            ServerIntegrationInstance server)
        {
            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));

            await RunTicksSync(client, server, 10);
        }

        /// <summary>
        ///     Runs <paramref name="ticks"/> ticks on both server and client while keeping their main loop in sync.
        /// </summary>
        protected static async Task RunTicksSync(ClientIntegrationInstance client, ServerIntegrationInstance server,
            int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }

        protected MapId GetMainMapId(IMapManager manager)
        {
            // TODO a heuristic that is not this bad
            return manager.GetAllMapIds().Last();
        }

        protected IMapGrid GetMainGrid(IMapManager manager)
        {
            // TODO a heuristic that is not this bad
            return manager.GetAllGrids().First();
        }

        protected TileRef GetMainTile(IMapGrid grid)
        {
            // TODO a heuristic that is not this bad
            return grid.GetAllTiles().First();
        }

        protected EntityCoordinates GetMainEntityCoordinates(IMapManager manager)
        {
            var gridId = GetMainGrid(manager).GridEntityId;
            return new EntityCoordinates(gridId, -0.5f, -0.5f);
        }

        protected sealed class ClientContentIntegrationOption : ClientIntegrationOptions
        {
            public ClientContentIntegrationOption()
            {
                FailureLogLevel = LogLevel.Warning;
            }

            public override GameControllerOptions Options { get; set; } = new()
            {
                LoadContentResources = true,
                LoadConfigAndUserData = false,
            };

            public Action ContentBeforeIoC { get; set; }
        }

        protected sealed class ServerContentIntegrationOption : ServerIntegrationOptions
        {
            public ServerContentIntegrationOption()
            {
                FailureLogLevel = LogLevel.Warning;
            }

            public override ServerOptions Options { get; set; } = new()
            {
                LoadContentResources = true,
                LoadConfigAndUserData = false,
            };

            public Action ContentBeforeIoC { get; set; }
        }
    }
}
