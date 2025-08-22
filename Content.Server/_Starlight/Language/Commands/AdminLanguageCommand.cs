using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._Starlight.Language.Commands;

[ToolshedCommand(Name = "language"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminLanguageCommand : ToolshedCommand
{
    private LanguageSystem? _languagesField;
    private LanguageSystem Languages => _languagesField ??= GetSys<LanguageSystem>();

    [CommandImplementation("add")]
    public EntityUid AddLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    )
    {
        if (language == SharedLanguageSystem.UniversalPrototype)
        {
            EnsureComp<UniversalLanguageSpeakerComponent>(input);
            Languages.UpdateEntityLanguages(input);
        }
        else
        {
            EnsureComp<LanguageSpeakerComponent>(input);
            Languages.AddLanguage(input, language, canSpeak, canUnderstand);
        }

        return input;
    }

    [CommandImplementation("rm")]
    public EntityUid RemoveLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool removeSpeak = true,
        [CommandArgument] bool removeUnderstand = true
    )
    {
        if (language == SharedLanguageSystem.UniversalPrototype && HasComp<UniversalLanguageSpeakerComponent>(input))
        {
            RemComp<UniversalLanguageSpeakerComponent>(input);
            EnsureComp<LanguageSpeakerComponent>(input);
        }
        // We execute this branch even in case of universal so that it gets removed if it was added manually to the LanguageKnowledge.
        Languages.RemoveLanguage(input, language, removeSpeak, removeUnderstand);

        return input;
    }

    [CommandImplementation("lsspoken")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListSpoken([PipedArgument] EntityUid input)
    {
        return Languages.GetSpokenLanguages(input);
    }

    [CommandImplementation("lsunderstood")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListUnderstood([PipedArgument] EntityUid input)
    {
        return Languages.GetUnderstoodLanguages(input);
    }
}