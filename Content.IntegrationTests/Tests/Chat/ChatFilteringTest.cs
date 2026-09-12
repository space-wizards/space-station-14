#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Client.UserInterface.Systems.Chat;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests.Chat;

public sealed class ChatFilteringTest : GameTest
{
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _configManager = null!;
    [SidedDependency(Side.Client)] private readonly IUserInterfaceManager _uiManager = null!;

    [Test]
    [RunOnSide(Side.Client)]
    public async Task TestCustomWordFiltersSaved()
    {
        var chatController = _uiManager.GetUIController<ChatUIController>();

        // 1. Set custom word filters
        var customFilters = "ling\nrev\n\"syndicate\"";
        chatController.UpdateWordFilters(customFilters);

        // 2. Verify they are saved to CVar
        Assert.That(_configManager.GetCVar(CCVars.ChatWordFilters), Is.EqualTo(customFilters));

        // 3. Verify internal active regex filters
        var filtersField = chatController.GetType().GetField(
            "_filters",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        Assert.That(filtersField, Is.Not.Null);
        var activeFilters = (List<Regex>)filtersField.GetValue(chatController)!;

        Assert.That(activeFilters.Count, Is.EqualTo(3));

        // Sorted by length descending: "syndicate" (9), "ling" (4), "rev" (3)
        var patterns = activeFilters.ConvertAll(r => r.ToString());
        Assert.That(patterns[0], Does.Contain("(?<!\\w)syndicate(?!\\w)"));
        Assert.That(patterns[1], Does.Contain("ling"));
        Assert.That(patterns[2], Does.Contain("rev"));
    }

    [Test]
    [RunOnSide(Side.Client)]
    public async Task TestWordFilteringMasksMessage()
    {
        var chatController = _uiManager.GetUIController<ChatUIController>();

        // 1. Set custom word filters: "ling", exact word "rev", and "Captain"
        var customFilters = "ling\n\"rev\"\nCaptain";
        chatController.UpdateWordFilters(customFilters);

        // 2. Create message with matches
        var originalText = "The Captain saw a ling and revolution!";
        var msg = new ChatMessage(
            ChatChannel.Local,
            originalText,
            originalText,
            default,
            null
        );

        // 3. Process the chat message
        chatController.ProcessChatMessage(msg);

        // 4. Assertions:
        // - "Captain" (7 chars) -> "*******"
        // - "ling" (4 chars) -> "****"
        // - "revolution" must NOT be masked because "rev" is quoted as whole word
        Assert.That(msg.WrappedMessage, Is.EqualTo("The ******* saw a **** and revolution!"));
    }

    [Test]
    [RunOnSide(Side.Client)]
    public async Task TestWordFilteringPreservesTagsAndCustomSymbol()
    {
        var chatController = _uiManager.GetUIController<ChatUIController>();

        // 1. Set custom filter symbol to '#'
        _configManager.SetCVar(CCVars.ChatWordFiltersSymbol, "#");

        // 2. Set custom word filters
        chatController.UpdateWordFilters("red\nfool");

        // 3. Create message with rich text tags
        var originalText = "[color=red]You red fool![/color]";
        var msg = new ChatMessage(
            ChatChannel.Local,
            originalText,
            originalText,
            default,
            null
        );

        // 4. Process the chat message
        chatController.ProcessChatMessage(msg);

        // 5. Assertions:
        // - Tag "[color=red]" must remain untouched
        // - "red" inside text -> "###"
        // - "fool" inside text -> "####"
        Assert.That(msg.WrappedMessage, Is.EqualTo("[color=red]You ### ####![/color]"));

        // Reset filter symbol back to default
        _configManager.SetCVar(CCVars.ChatWordFiltersSymbol, "*");
    }
}
