using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Components;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

public sealed partial class BiomeSystem
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class BiomeAddLayerCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        public override string Command => "biome_addlayer";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteError(Help);
                return;
            }

            if (!int.TryParse(args[0], out var entInt))
            {
                return;
            }

            var entId = new EntityUid(entInt);

            if (!EntityManager.TryGetComponent(entId, out BiomeComponent? biome))
            {
                return;
            }

            if (!_protoManager.TryIndex(args[2], out DungeonConfigPrototype? config))
            {
                return;
            }

            var system = EntityManager.System<BiomeSystem>();
            system.AddLayer((entId, biome), args[1], config);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromOptions(CompletionHelper.Components<BiomeComponent>(args[0]));
                case 2:
                    return CompletionResult.FromHint("layerId");
                case 3:
                    return CompletionResult.FromOptions(CompletionHelper.PrototypeIDs<DungeonConfigPrototype>());
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Mapping)]
    public sealed class BiomeRemoveLayerCommand : LocalizedEntityCommands
    {
        public override string Command => "biome_removelayer";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Help);
                return;
            }

            if (!int.TryParse(args[0], out var entInt))
            {
                return;
            }

            var entId = new EntityUid(entInt);

            if (!EntityManager.TryGetComponent(entId, out BiomeComponent? biome))
            {
                return;
            }

            var system = EntityManager.System<BiomeSystem>();
            system.RemoveLayer((entId, biome), args[1]);
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            switch (args.Length)
            {
                case 0:
                    return CompletionResult.FromOptions(CompletionHelper.Components<BiomeComponent>(args[0]));
                case 1:
                    return CompletionResult.FromHint("layerId");
            }

            return CompletionResult.Empty;
        }
    }
}
