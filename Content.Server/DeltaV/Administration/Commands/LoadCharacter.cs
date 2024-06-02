using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

// This literally only exists because haha felinid oni
namespace Content.Server.DeltaV.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class LoadCharacter : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entitySys = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    public string Command => "loadcharacter";
    public string Description => Loc.GetString("loadcharacter-command-description");
    public string Help => Loc.GetString("loadcharacter-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not ICommonSession player)
        {
            shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        var data = player.ContentData();

        if (data == null)
        {
            shell.WriteError(Loc.GetString("shell-entity-is-not-mob")); // No mind specific errors? :(
            return;
        }

        EntityUid target;

        if (args.Length >= 1)
        {
            if (!EntityUid.TryParse(args.First(), out var uid))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            target = uid;
        }
        else
        {
            if (player.AttachedEntity == null ||
                !_entityManager.HasComponent<HumanoidAppearanceComponent>(player.AttachedEntity.Value))
            {
                shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
                return;
            }

            target = player.AttachedEntity.Value;
        }

        if (!target.IsValid() || !_entityManager.EntityExists(target))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        if (!_entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoidAppearance))
        {
            shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", target.ToString()),
                ("componentName", nameof(HumanoidAppearanceComponent))));
            return;
        }

        HumanoidCharacterProfile character;

        if (args.Length >= 2)
        {
            // This seems like a bad way to go about it, but it works so eh?
            var name = string.Join(" ", args.Skip(1).ToArray());
            shell.WriteLine(Loc.GetString("loadcharacter-command-fetching", ("name", name)));

            if (!FetchCharacters(data.UserId, out var characters))
            {
                shell.WriteError(Loc.GetString("loadcharacter-command-failed-fetching"));
                return;
            }

            var selectedCharacter = characters.FirstOrDefault(c => c.Name == name);

            if (selectedCharacter == null)
            {
                shell.WriteError(Loc.GetString("loadcharacter-command-failed-fetching"));
                return;
            }

            character = selectedCharacter;
        }
        else
            character = (HumanoidCharacterProfile) _prefs.GetPreferences(data.UserId).SelectedCharacter;

        // This shouldn't ever fail considering the previous checks
        if (!_prototypeManager.TryIndex(humanoidAppearance.Species, out var speciesPrototype) ||
            !_prototypeManager.TryIndex<SpeciesPrototype>(character.Species, out var entPrototype))
            return;

        if (speciesPrototype != entPrototype)
            shell.WriteLine(Loc.GetString("loadcharacter-command-mismatch"));

        var coordinates = player.AttachedEntity != null
            ? _entityManager.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
            : EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();

        EntitySystem.Get<StationSpawningSystem>()
            .SpawnPlayerMob(coordinates, profile: character, entity: target, job: null, station: null);

        shell.WriteLine(Loc.GetString("loadcharacter-command-complete"));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHint(Loc.GetString("shell-argument-uid"));
            case 2:
            {
                if (shell.Player is not ICommonSession player)
                    return CompletionResult.Empty;

                var data = player.ContentData();
                var mind = data?.Mind;

                if (mind == null || data == null)
                    return CompletionResult.Empty;

                return FetchCharacters(data.UserId, out var characters)
                    ? CompletionResult.FromOptions(characters.Select(c => c.Name))
                    : CompletionResult.Empty;
            }
            default:
                return CompletionResult.Empty;
        }
    }

    private bool FetchCharacters(NetUserId player, out HumanoidCharacterProfile[] characters)
    {
        characters = null!;
        if (!_prefs.TryGetCachedPreferences(player, out var prefs))
            return false;

        characters = prefs.Characters
            .Where(kv => kv.Value is HumanoidCharacterProfile)
            .Select(kv => (HumanoidCharacterProfile) kv.Value)
            .ToArray();

        return true;
    }
}
