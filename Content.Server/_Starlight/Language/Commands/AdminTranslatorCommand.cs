using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Starlight.Language;
using Content.Shared._Starlight.Language.Components;
using Content.Shared._Starlight.Language.Components.Translators;
using Content.Shared._Starlight.Language.Systems;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server._Starlight.Language.Commands;

[ToolshedCommand(Name = "translator"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminTranslatorCommand : ToolshedCommand
{
    private LanguageSystem? _languagesField;
    private ContainerSystem? _containersField;

    private ContainerSystem Containers => _containersField ??= GetSys<ContainerSystem>();
    private LanguageSystem Languages => _languagesField ??= GetSys<LanguageSystem>();

    [CommandImplementation("addlang")]
    public EntityUid AddLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool addSpeak = true,
        [CommandArgument] bool addUnderstand = true
    )
    {
        if (language == SharedLanguageSystem.UniversalPrototype)
            throw new ArgumentException(Loc.GetString("command-language-error-this-will-not-work"));

        if (!TryGetTranslatorComp(input, out var translator))
            throw new ArgumentException(Loc.GetString("command-language-error-not-a-translator", ("entity", input)));

        if (addSpeak && !translator.SpokenLanguages.Contains(language))
            translator.SpokenLanguages.Add(language);
        if (addUnderstand && !translator.UnderstoodLanguages.Contains(language))
            translator.UnderstoodLanguages.Add(language);

        UpdateTranslatorHolder(input);

        return input;
    }

    [CommandImplementation("rmlang")]
    public EntityUid RemoveLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool removeSpeak = true,
        [CommandArgument] bool removeUnderstand = true
    )
    {
        if (!TryGetTranslatorComp(input, out var translator))
            throw new ArgumentException(Loc.GetString("command-language-error-not-a-translator", ("entity", input)));

        if (removeSpeak)
            translator.SpokenLanguages.Remove(language);
        if (removeUnderstand)
            translator.UnderstoodLanguages.Remove(language);

        UpdateTranslatorHolder(input);

        return input;
    }

    [CommandImplementation("addrequired")]
    public EntityUid AddRequiredLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language)
    {
        if (!TryGetTranslatorComp(input, out var translator))
            throw new ArgumentException(Loc.GetString("command-language-error-not-a-translator", ("entity", input)));

        if (!translator.RequiredLanguages.Contains(language))
        {
            translator.RequiredLanguages.Add(language);
            UpdateTranslatorHolder(input);
        }

        return input;
    }

    [CommandImplementation("rmrequired")]
    public EntityUid RemoveRequiredLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language)
    {
        if (!TryGetTranslatorComp(input, out var translator))
            throw new ArgumentException(Loc.GetString("command-language-error-not-a-translator", ("entity", input)));

        if (translator.RequiredLanguages.Remove(language))
            UpdateTranslatorHolder(input);

        return input;
    }

    [CommandImplementation("lsspoken")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListSpoken([PipedArgument] EntityUid input)
    {
        if (!TryGetTranslatorComp(input, out var translator))
            return [];
        return translator.SpokenLanguages;
    }

    [CommandImplementation("lsunderstood")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListUnderstood([PipedArgument] EntityUid input)
    {
        if (!TryGetTranslatorComp(input, out var translator))
            return [];
        return translator.UnderstoodLanguages;
    }

    [CommandImplementation("lsrequired")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListRequired([PipedArgument] EntityUid input)
    {
        if (!TryGetTranslatorComp(input, out var translator))
            return [];
        return translator.RequiredLanguages;
    }

    private bool TryGetTranslatorComp(EntityUid uid, [NotNullWhen(true)] out BaseTranslatorComponent? translator)
    {
        if (TryComp<HandheldTranslatorComponent>(uid, out var handheld))
            translator = handheld;
        else if (TryComp<TranslatorImplantComponent>(uid, out var implant))
            translator = implant;
        else if (TryComp<IntrinsicTranslatorComponent>(uid, out var intrinsic))
            translator = intrinsic;
        else
            translator = null;

        return translator != null;
    }

    private void UpdateTranslatorHolder(EntityUid translator)
    {
        if (!Containers.TryGetContainingContainer(translator, out var cont)
            || cont.Owner is not { Valid: true } holder)
            return;

        Languages.UpdateEntityLanguages(holder);
    }
}