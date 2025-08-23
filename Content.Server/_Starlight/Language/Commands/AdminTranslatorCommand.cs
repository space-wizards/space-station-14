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
using System.Linq;

namespace Content.Server._Starlight.Language.Commands;

[ToolshedCommand(Name = "translator"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminTranslatorCommand : ToolshedCommand
{
    private LanguageSystem? _language;
    private ContainerSystem? _containers;

    [CommandImplementation("addlang")]
    public EntityUid AddLanguage(
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool addSpeak = true,
        [CommandArgument] bool addUnderstand = true
    )
    {
        _language ??= GetSys<LanguageSystem>();
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

    [CommandImplementation("addlang")]
    public IEnumerable<EntityUid> AddLanguage(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    ) => input.Select(x => AddLanguage(x, language, canSpeak, canUnderstand));

    [CommandImplementation("rmlang")]
    public EntityUid RemoveLanguage(
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

    [CommandImplementation("rmlang")]
    public IEnumerable<EntityUid> RemoveLanguage(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    ) => input.Select(x => RemoveLanguage(x, language, canSpeak, canUnderstand));

    [CommandImplementation("addrequired")]
    public EntityUid AddRequiredLanguage(
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

    [CommandImplementation("addrequired")]
    public IEnumerable<EntityUid> AddRequiredLanguage(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<LanguagePrototype> language
    ) => input.Select(x => AddRequiredLanguage(x, language));

    [CommandImplementation("rmrequired")]
    public EntityUid RemoveRequiredLanguage(
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language)
    {
        if (!TryGetTranslatorComp(input, out var translator))
            throw new ArgumentException(Loc.GetString("command-language-error-not-a-translator", ("entity", input)));

        if (translator.RequiredLanguages.Remove(language))
            UpdateTranslatorHolder(input);

        return input;
    }

    [CommandImplementation("rmrequired")]
    public IEnumerable<EntityUid> RemoveRequiredLanguage(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<LanguagePrototype> language
    ) => input.Select(x => RemoveRequiredLanguage(x, language));

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
        _language ??= GetSys<LanguageSystem>();
        _containers ??= GetSys<ContainerSystem>();
        if (!_containers.TryGetContainingContainer(translator, out var cont)
            || cont.Owner is not { Valid: true } holder)
            return;

        _language.UpdateEntityLanguages(holder);
    }
}