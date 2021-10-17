// using System;
// using NUnit.Framework;
// using Robust.UnitTesting;
//
// namespace Content.IntegrationTests
// {
//     [SetUpFixture]
//     public class ContentGlobalSetup
//     {
//         [OneTimeSetUp]
//         public void SetUp()
//         {
//             var processors = Environment.ProcessorCount;
//             var test = new DummyTest();
//
//             for (var i = 0; i < 1; i++)
//             {
//                 RobustIntegrationTest.ClientsReady.Enqueue(test.CreateClient());
//                 RobustIntegrationTest.ServersReady.Enqueue(test.CreateServer());
//             }
//         }
//
//         private class DummyTest : ContentIntegrationTest
//         {
//             public ClientIntegrationInstance CreateClient()
//             {
//                 return StartClient();
//             }
//
//             public ServerIntegrationInstance CreateServer()
//             {
//                 return StartServer();
//             }
//         }
//     }
// }
