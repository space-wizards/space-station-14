using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Content.Client.CharacterInfo;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests.Chat
{
    [TestFixture]
    public sealed class ChatHighlightTest
    {
        [Test]
        public async Task TestCustomHighlightsPreserved()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                Connected = true,
            });

            var client = pair.Client;
            var configManager = client.ResolveDependency<IConfigurationManager>();
            var uiManager = client.ResolveDependency<IUserInterfaceManager>();

            await client.WaitPost(() =>
            {
                var chatController = uiManager.GetUIController<ChatUIController>();

                // 1. Enable auto-fill highlights
                configManager.SetCVar(CCVars.ChatAutoFillHighlights, true);

                // 2. Set custom highlights
                var customHighlights = "ling\nrev";
                chatController.UpdateHighlights(customHighlights);

                // Verify they are saved
                Assert.That(configManager.GetCVar(CCVars.ChatHighlights), Is.EqualTo(customHighlights));

                // 3. Simulate character update
                var characterData = new CharacterInfoSystem.CharacterData(
                    default,
                    "Captain",
                    new Dictionary<string, List<Shared.Objectives.ObjectiveInfo>>(),
                    null,
                    "John Doe"
                );

                var method = chatController.GetType().GetMethod(
                    "OnCharacterUpdated",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                Assert.That(method, Is.Not.Null);

                // Set internal state to allow character update processing
                var attachField = chatController.GetType().GetField(
                    "_charInfoIsAttach",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Assert.That(attachField, Is.Not.Null);
                attachField.SetValue(chatController, true);

                // Invoke update
                method.Invoke(chatController, new object[] { characterData });

                // 4. Assertions:
                // - Custom highlights in config must remain unchanged
                Assert.That(configManager.GetCVar(CCVars.ChatHighlights), Is.EqualTo(customHighlights));

                // - Internal active regex highlights must contain both custom & auto-filled highlights
                var highlightsField = chatController.GetType().GetField(
                    "_highlights",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Assert.That(highlightsField, Is.Not.Null);
                var activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;

                // Check that custom and auto highlights are loaded
                // Custom:
                Assert.That(activeHighlights, Contains.Item("ling"));
                Assert.That(activeHighlights, Contains.Item("rev"));
                // Auto:
                Assert.That(activeHighlights, Contains.Item("Captain"));
                Assert.That(activeHighlights, Contains.Item("(?<!\\w)Cap(?!\\w)")); // "Cap" becomes regex-escaped and word-bounded

                // 5. Disable auto-fill highlights and verify auto-filled highlights are removed
                configManager.SetCVar(CCVars.ChatAutoFillHighlights, false);

                activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;
                Assert.That(activeHighlights, Contains.Item("ling"));
                Assert.That(activeHighlights, Contains.Item("rev"));
                Assert.That(activeHighlights, Is.Not.Contains("Captain"));
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task TestEnablingAutoFillPreservesCustomHighlights()
        {
            await using var pair = await PoolManager.GetServerClient(new PoolSettings
            {
                Connected = true,
            });

            var client = pair.Client;
            var configManager = client.ResolveDependency<IConfigurationManager>();
            var uiManager = client.ResolveDependency<IUserInterfaceManager>();

            await client.WaitPost(() =>
            {
                var chatController = uiManager.GetUIController<ChatUIController>();

                // 1. Start with auto-fill disabled
                configManager.SetCVar(CCVars.ChatAutoFillHighlights, false);

                // 2. Set custom highlights
                var customHighlights = "ling\nrev";
                chatController.UpdateHighlights(customHighlights);

                // Verify active matches are ONLY custom highlights
                var highlightsField = chatController.GetType().GetField(
                    "_highlights",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Assert.That(highlightsField, Is.Not.Null);
                var activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;

                Assert.That(activeHighlights, Contains.Item("ling"));
                Assert.That(activeHighlights, Contains.Item("rev"));
                Assert.That(activeHighlights.Count, Is.EqualTo(2));

                // 3. Enable auto-fill highlights
                configManager.SetCVar(CCVars.ChatAutoFillHighlights, true);

                // 4. Simulate character update (spawning into round)
                var characterData = new CharacterInfoSystem.CharacterData(
                    default,
                    "Captain",
                    new Dictionary<string, List<Shared.Objectives.ObjectiveInfo>>(),
                    null,
                    "John Doe"
                );

                var method = chatController.GetType().GetMethod(
                    "OnCharacterUpdated",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                Assert.That(method, Is.Not.Null);

                var attachField = chatController.GetType().GetField(
                    "_charInfoIsAttach",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                Assert.That(attachField, Is.Not.Null);
                attachField.SetValue(chatController, true);

                // Invoke character update
                method.Invoke(chatController, new object[] { characterData });

                // 5. Assertions:
                // - Config highlights MUST NOT be wiped and remain as custom highlights
                Assert.That(configManager.GetCVar(CCVars.ChatHighlights), Is.EqualTo(customHighlights));

                // - Active highlights list must now merge both custom and auto-filled ones
                activeHighlights = (List<string>)highlightsField.GetValue(chatController)!;
                Assert.That(activeHighlights, Contains.Item("ling"));
                Assert.That(activeHighlights, Contains.Item("rev"));
                Assert.That(activeHighlights, Contains.Item("Captain"));
                Assert.That(activeHighlights, Contains.Item("(?<!\\w)Cap(?!\\w)"));
            });

            await pair.CleanReturnAsync();
        }
    }
}
