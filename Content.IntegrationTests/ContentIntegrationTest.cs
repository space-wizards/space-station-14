using System;
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
using Robust.Shared.Network;
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

        protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions? options = null)
        {
            options ??= new ServerContentIntegrationOption()
            {
                FailureLogLevel = LogLevel.Warning,
                Pool = true
            };

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

            // Avoid funny race conditions with the database.
            options.CVarOverrides[CCVars.DatabaseSynchronous.Name] = "true";

            // Disable holidays as some of them might mess with the map at round start.
            options.CVarOverrides[CCVars.HolidaysEnabled.Name] = "false";

            // Avoid loading a large map by default for integration tests if none has been specified.
            if(!options.CVarOverrides.ContainsKey(CCVars.GameMap.Name))
                options.CVarOverrides[CCVars.GameMap.Name] = "Maps/Test/empty.yml";

            return base.StartServer(options);
        }

        protected ServerIntegrationInstance StartServerDummyTicker(ServerIntegrationOptions options = null)
        {
            options ??= new ServerContentIntegrationOption
            {
                Pool = true
            };

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

        protected override async Task TearDown(ServerIntegrationInstance server)
        {
            await server.WaitIdleAsync();

            var systems = server.ResolveDependency<IEntitySystemManager>();

            await server.WaitPost(() =>
            {
                systems.GetEntitySystem<GameTicker>().RestartRound();
            });

            await server.WaitRunTicks(3);
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
