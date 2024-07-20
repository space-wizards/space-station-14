using Content.Server.Administration;
using Content.Server.Cargo.Systems;
using Content.Shared.Administration;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Console;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem : SharedXenoArtifactSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoArtifactComponent, MapInitEvent>(OnArtifactMapInit);
        SubscribeLocalEvent<XenoArtifactComponent, PriceCalculationEvent>(OnCalculatePrice);

        // this isn't a toolshed command because apparently it doesn't support autocompletion.
        // and no, i'm not writing some complicated bullshit for a one-off testing command.
        _consoleHost.RegisterCommand("unlocknode",
            Loc.GetString("cmd-unlocknode-desc"),
            Loc.GetString("cmd-unlocknode-help"),
            UnlockNodeCommand,
            UnlockNodeCompletion);
    }

    [AdminCommand(AdminFlags.Debug)]
    private void UnlockNodeCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            Log.Error("incorrect number of args!");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var netNode))
        {
            Log.Error("invalid node entity");
            return;
        }

        SetNodeUnlocked(GetEntity(netNode));
    }

    private CompletionResult UnlockNodeCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.Components<XenoArtifactComponent>(args[0]), "<artifact uid>");
        }

        if (args.Length == 2 &&
            NetEntity.TryParse(args[0], out var netEnt) &&
            GetEntity(netEnt) is var ent &&
            TryComp<XenoArtifactComponent>(ent, out var comp))
        {
            var result = new List<CompletionOption>();
            foreach (var node in GetAllNodes((ent, comp)))
            {
                result.Add(new CompletionOption(GetNetEntity(node).ToString(), Name(node)));
            }

            return CompletionResult.FromHintOptions(result, "<node uid>");
        }
        return CompletionResult.Empty;
    }

    private void OnArtifactMapInit(Entity<XenoArtifactComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.DoGeneration)
            GenerateArtifactStructure(ent);
    }

    private void OnCalculatePrice(Entity<XenoArtifactComponent> ent, ref PriceCalculationEvent args)
    {
        foreach (var node in GetAllNodes(ent))
        {
            if (node.Comp.Locked)
                continue;

            args.Price += node.Comp.ResearchValue * ent.Comp.PriceMultiplier;
        }
    }
}
