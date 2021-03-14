using System;
using Content.Client;
using Content.Client.Interfaces.Parallax;
using Content.Server;
using Content.Server.Interfaces.GameTicking;
using Content.Shared;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.ContentPack;
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
            options ??= new ClientIntegrationOptions();
            // ReSharper disable once RedundantNameQualifier
            options.ContentAssemblies = new[]
            {
                typeof(Shared.EntryPoint).Assembly,
                typeof(Client.EntryPoint).Assembly,
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

            // Connecting to Discord is a massive waste of time.
            // Basically just makes the CI logs a mess.
            options.CVarOverrides["discord.enabled"] = "false";

            // Avoid preloading textures in tests.
            options.CVarOverrides.TryAdd(CVars.TexturePreloadingEnabled.Name, "false");

            return base.StartClient(options);
        }

        protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions options = null)
        {
            options ??= new ServerIntegrationOptions();
            options.ContentAssemblies = new[]
            {
                typeof(Shared.EntryPoint).Assembly,
                typeof(Server.EntryPoint).Assembly,
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

            // Avoid loading a large map by default for integration tests.
            options.CVarOverrides[CCVars.GameMap.Name] = "Maps/Test/empty.yml";

            return base.StartServer(options);
        }

        protected ServerIntegrationInstance StartServerDummyTicker(ServerIntegrationOptions options = null)
        {
            options ??= new ServerIntegrationOptions();
            options.BeforeStart += () =>
            {
                IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ServerModuleTestingCallbacks
                {
                    ServerBeforeIoC = () =>
                    {
                        IoCManager.Register<IGameTicker, DummyGameTicker>(true);
                    }
                });
            };

            return StartServer(options);
        }

        protected (ClientIntegrationInstance client, ServerIntegrationInstance server)
            StartConnectedServerClientPair(ClientIntegrationOptions clientOptions = null,
                ServerIntegrationOptions serverOptions = null)
        {
            var client = StartClient(clientOptions);
            var server = StartServer(serverOptions);

            StartConnectedPairShared(client, server);

            return (client, server);
        }


        protected (ClientIntegrationInstance client, ServerIntegrationInstance server)
            StartConnectedServerDummyTickerClientPair(ClientIntegrationOptions clientOptions = null,
                ServerIntegrationOptions serverOptions = null)
        {
            var client = StartClient(clientOptions);
            var server = StartServerDummyTicker(serverOptions);

            StartConnectedPairShared(client, server);

            return (client, server);
        }

        protected void WaitUntil(IntegrationInstance instance, Func<bool> func, int maxTicks = 600,
            int tickStep = 1)
        {
            var ticksAwaited = 0;
            bool passed;

            instance.WaitIdleAsync();

            while (!(passed = func()) && ticksAwaited < maxTicks)
            {
                var ticksToRun = tickStep;

                if (ticksAwaited + tickStep > maxTicks)
                {
                    ticksToRun = maxTicks - ticksAwaited;
                }

                instance.WaitRunTicks(ticksToRun);

                ticksAwaited += ticksToRun;
            }

            Assert.That(passed);
        }

        private static void StartConnectedPairShared(ClientIntegrationInstance client,
            ServerIntegrationInstance server)
        {
            client.SetConnectTarget(server);

            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));

            RunTicksSync(client, server, 10);
        }

        /// <summary>
        ///     Runs <paramref name="ticks"/> ticks on both server and client while keeping their main loop in sync.
        /// </summary>
        protected static void RunTicksSync(ClientIntegrationInstance client, ServerIntegrationInstance server,
            int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                server.WaitRunTicks(1);
                client.WaitRunTicks(1);
            }
        }

        protected sealed class ClientContentIntegrationOption : ClientIntegrationOptions
        {
            public Action ContentBeforeIoC { get; set; }
        }

        protected sealed class ServerContentIntegrationOption : ServerIntegrationOptions
        {
            public Action ContentBeforeIoC { get; set; }
        }
    }
}
