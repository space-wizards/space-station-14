using Content.Shared.Administration;
using Content.Shared._Starlight.Language;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Starlight.Language.Commands;

[AnyCommand]
public sealed class ListLanguagesCommand : IConsoleCommand
{
    public string Command => "languagelist";
    public string Description => Loc.GetString("command-list-langs-desc");
    public string Help => Loc.GetString("command-list-langs-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not {} playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        var languages = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();
        var currentLang = languages.GetLanguage(playerEntity).ID;

        shell.WriteLine(Loc.GetString("command-language-spoken"));
        var spoken = languages.GetSpokenLanguages(playerEntity);
        for (int i = 0; i < spoken.Count; i++)
        {
            var lang = spoken[i];
            shell.WriteLine(lang == currentLang
                ? Loc.GetString("command-language-current-entry", ("id", i + 1), ("language", lang), ("name", LanguageName(lang)))
                : Loc.GetString("command-language-entry", ("id", i + 1), ("language", lang), ("name", LanguageName(lang))));
        }

        shell.WriteLine(Loc.GetString("command-language-understood"));
        var understood = languages.GetUnderstoodLanguages(playerEntity);
        for (int i = 0; i < understood.Count; i++)
        {
            var lang = understood[i];
            shell.WriteLine(Loc.GetString("command-language-entry", ("id", i + 1), ("language", lang), ("name", LanguageName(lang))));
        }
    }

    private string LanguageName(string id)
    {
        return Loc.GetString($"language-{id}-name");
    }
}
