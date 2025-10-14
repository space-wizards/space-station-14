using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Administration;
using Content.Shared.GameTicking.Components;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server.GameTicking.Rules;

[AdminCommand(AdminFlags.Fun)]
public sealed class TestRoundstartSoundCommand : IConsoleCommand
{
    public string Command => "testroundstartsound";
    public string Description => "Test roundstart sound playback";
    public string Help => "testroundstartsound [sound_path] [volume]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 0 && args[0] == "list")
        {
            // List all active RoundstartPlaySoundRule components
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var query = entityManager.AllEntityQueryEnumerator<RoundstartPlaySoundRuleComponent, GameRuleComponent>();
            var count = 0;
            
            shell.WriteLine("Active RoundstartPlaySoundRule components:");
            while (query.MoveNext(out var uid, out var comp, out var gameRule))
            {
                count++;
                var isActive = entityManager.System<GameTicker>().IsGameRuleActive(uid, gameRule);
                var hasEnded = entityManager.HasComponent<EndedGameRuleComponent>(uid);
                shell.WriteLine($"  {count}. Entity: {uid}, Sound: {comp.Sound}, Volume: {comp.Volume}, Active: {isActive}, Ended: {hasEnded}");
            }
            
            if (count == 0)
                shell.WriteLine("  No RoundstartPlaySoundRule components found!");
            else
                shell.WriteLine($"  Total: {count} components");
            return;
        }

        var soundPath = "/Audio/Weapons/counterstrike/other/roundstart/cs.ogg";
        var volume = -8f;

        if (args.Length > 0)
            soundPath = args[0];
        
        if (args.Length > 1 && float.TryParse(args[1], out var vol))
            volume = vol;

        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RoundstartPlaySoundRuleSystem>();
        system.TestPlaySound(soundPath, volume);
        
        shell.WriteLine($"Testing sound: {soundPath} at volume {volume}");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint("Sound path (e.g., /Audio/Weapons/counterstrike/other/roundstart/cs.ogg)");
        }
        
        if (args.Length == 2)
        {
            return CompletionResult.FromHint("Volume (e.g., -8)");
        }

        return CompletionResult.Empty;
    }
}
