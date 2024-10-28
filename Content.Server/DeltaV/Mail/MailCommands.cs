using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Administration;
using Content.Server.Administration;
using Content.Server.DeltaV.Mail.Components;
using Content.Server.DeltaV.Mail.EntitySystems;

namespace Content.Server.DeltaV.Mail;

[AdminCommand(AdminFlags.Fun)]
public sealed class MailToCommand : IConsoleCommand
{
    public string Command => "mailto";
    public string Description => Loc.GetString("command-mailto-description", ("requiredComponent", nameof(MailReceiverComponent)));
    public string Help => Loc.GetString("command-mailto-help", ("command", Command));

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private const string BlankMailPrototype = "MailAdminFun";
    private const string BlankLargeMailPrototype = "MailLargeAdminFun"; // Frontier: large mail
    private const string Container = "storagebase";
    private const string MailContainer = "contents";


    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 4)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!EntityUid.TryParse(args[0], out var recipientUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!EntityUid.TryParse(args[1], out var containerUid))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!bool.TryParse(args[2], out var isFragile))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        if (!bool.TryParse(args[3], out var isPriority))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }

        // Frontier: Large Mail
        var isLarge = false;
        if (args.Length > 4 && !bool.TryParse(args[4], out isLarge))
        {
            shell.WriteError(Loc.GetString("shell-invalid-bool"));
            return;
        }
        var mailPrototype = isLarge ? BlankLargeMailPrototype : BlankMailPrototype;
        // End Frontier


        var mailSystem = _entitySystemManager.GetEntitySystem<MailSystem>();
        var containerSystem = _entitySystemManager.GetEntitySystem<SharedContainerSystem>();

        if (!_entityManager.HasComponent<MailReceiverComponent>(recipientUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailreceiver", ("requiredComponent", nameof(MailReceiverComponent))));
            return;
        }

        if (!_prototypeManager.HasIndex<EntityPrototype>(mailPrototype)) // Frontier: _blankMailPrototype<mailPrototype
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-blankmail", ("blankMail", mailPrototype))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        if (!containerSystem.TryGetContainer(containerUid, Container, out var targetContainer))
        {
            shell.WriteLine(Loc.GetString("command-mailto-invalid-container", ("requiredContainer", Container)));
            return;
        }

        if (!mailSystem.TryGetMailRecipientForReceiver(recipientUid, out var recipient))
        {
            shell.WriteLine(Loc.GetString("command-mailto-unable-to-receive"));
            return;
        }

        if (!mailSystem.TryGetMailTeleporterForReceiver(recipientUid, out var teleporterComponent, out var teleporterUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-teleporter-found"));
            return;
        }

        var mailUid = _entityManager.SpawnEntity(mailPrototype, _entityManager.GetComponent<TransformComponent>(containerUid).Coordinates); // Frontier: _blankMailPrototype<mailPrototype
        var mailContents = containerSystem.EnsureContainer<Container>(mailUid, MailContainer);

        if (!_entityManager.TryGetComponent<MailComponent>(mailUid, out var mailComponent))
        {
            shell.WriteLine(Loc.GetString("command-mailto-bogus-mail", ("blankMail", mailPrototype), ("requiredMailComponent", nameof(MailComponent)))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        foreach (var entity in targetContainer.ContainedEntities.ToArray())
        {
            containerSystem.Insert(entity, mailContents);
        }

        mailComponent.IsFragile = isFragile;
        mailComponent.IsPriority = isPriority;
        mailComponent.IsLarge = isLarge; //Frontier Mail

        mailSystem.SetupMail(mailUid, teleporterComponent, recipient.Value);

        var teleporterQueue = containerSystem.EnsureContainer<Container>((EntityUid)teleporterUid, "queued");
        containerSystem.Insert(mailUid, teleporterQueue);
        shell.WriteLine(Loc.GetString("command-mailto-success", ("timeToTeleport", teleporterComponent.TeleportInterval.TotalSeconds - teleporterComponent.Accumulator)));
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class MailNowCommand : IConsoleCommand
{
    public string Command => "mailnow";
    public string Description => Loc.GetString("command-mailnow");
    public string Help => Loc.GetString("command-mailnow-help", ("command", Command));

    [Dependency] private readonly IEntityManager _entityManager = default!;

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        foreach (var mailTeleporter in _entityManager.EntityQuery<MailTeleporterComponent>())
        {
            mailTeleporter.Accumulator += (float) mailTeleporter.TeleportInterval.TotalSeconds - mailTeleporter.Accumulator;
        }

        shell.WriteLine(Loc.GetString("command-mailnow-success"));
    }
}
