using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using System.Linq;

namespace Content.Server._Starlight.Language.Commands;

[ToolshedCommand(Name = "language"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminLanguageCommand : ToolshedCommand
{
    private LanguageSystem? _languages;

    [CommandImplementation("add")]
    public EntityUid AddLanguage(
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    )
    {
        _languages ??= GetSys<LanguageSystem>();
        if (language == SharedLanguageSystem.UniversalPrototype)
        {
            EnsureComp<UniversalLanguageSpeakerComponent>(input);
            _languages.UpdateEntityLanguages(input);
        }
        else
        {
            EnsureComp<LanguageSpeakerComponent>(input);
            _languages.AddLanguage(input, language, canSpeak, canUnderstand);
        }

        return input;
    }

    [CommandImplementation("add")]
    public IEnumerable<EntityUid> AddLanguage(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    ) => input.Select(x => AddLanguage(x, language, canSpeak, canUnderstand));

    [CommandImplementation("rm")]
    public EntityUid RemoveLanguage(
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool removeSpeak = true,
        [CommandArgument] bool removeUnderstand = true
    )
    {
        _languages ??= GetSys<LanguageSystem>();
        if (language == SharedLanguageSystem.UniversalPrototype && HasComp<UniversalLanguageSpeakerComponent>(input))
        {
            RemComp<UniversalLanguageSpeakerComponent>(input);
            EnsureComp<LanguageSpeakerComponent>(input);
        }
        // We execute this branch even in case of universal so that it gets removed if it was added manually to the LanguageKnowledge.
        _languages.RemoveLanguage(input, language, removeSpeak, removeUnderstand);

        return input;
    }

    [CommandImplementation("rm")]
    public IEnumerable<EntityUid> RemoveLanguage(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    ) => input.Select(x => RemoveLanguage(x, language, canSpeak, canUnderstand));

    [CommandImplementation("lsspoken")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListSpoken([PipedArgument] EntityUid input)
    {
        _languages ??= GetSys<LanguageSystem>();
        return _languages.GetSpokenLanguages(input);
    }

    [CommandImplementation("lsunderstood")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListUnderstood([PipedArgument] EntityUid input)
    {
        _languages ??= GetSys<LanguageSystem>();
        return _languages.GetUnderstoodLanguages(input);
    }
}