using Content.Server.DeadSpace.ERTCall;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Spawn)]
    public sealed class AcceptERTCommand : IConsoleCommand
    {
        public string Command => "ert_accept";
        public string Description => Loc.GetString("accept-ert-command-description");
        public string Help => Loc.GetString("accept-ert-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as ICommonSession;

            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var stationUid = EntitySystem.Get<StationSystem>().GetOwningStation(player.AttachedEntity.Value);

            if (stationUid == null)
            {
                shell.WriteLine(Loc.GetString("ert-command-invalid-grid"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetComponent<ERTCallComponent>(stationUid, out var component))
            {
                shell.WriteLine(Loc.GetString("ert-command-no-component"));
                return;
            }

            if (component.ERTCalled == false)
            {
                shell.WriteLine(Loc.GetString("ert-command-no-ert-called"));
                return;
            }

            if (component.ERTCalledTeam == null)
            {
                shell.WriteLine(Loc.GetString("ert-command-no-ert-called-in-component"));
                return;
            }

            EntitySystem.Get<ERTCallSystem>().Accept(component, player.Name);
        }
    }

    [AdminCommand(AdminFlags.Mapping)]
    public sealed class FakeAcceptERTCommand : IConsoleCommand
    {
        public string Command => "ert_fake_accept";
        public string Description => Loc.GetString("fake-accept-ert-command-description");
        public string Help => Loc.GetString("fake-accept-ert-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as ICommonSession;

            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var stationUid = EntitySystem.Get<StationSystem>().GetOwningStation(player.AttachedEntity.Value);

            if (stationUid == null)
            {
                shell.WriteLine(Loc.GetString("ert-command-invalid-grid"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetComponent<ERTCallComponent>(stationUid, out var component))
            {
                shell.WriteLine(Loc.GetString("ert-command-no-component"));
                return;
            }

            if (component.ERTCalled == false)
            {
                shell.WriteLine(Loc.GetString("ert-command-no-ert-called"));
                return;
            }

            if (component.ERTCalledTeam == null)
            {
                shell.WriteLine(Loc.GetString("ert-command-no-ert-called-in-component"));
                return;
            }

            EntitySystem.Get<ERTCallSystem>().FakeAccept(component, player.Name);
        }
    }

    [AdminCommand(AdminFlags.Spawn)]
    public sealed class RefuseERTCommand : IConsoleCommand
    {
        public string Command => "ert_refuse";
        public string Description => Loc.GetString("refuse-ert-command-description");
        public string Help => Loc.GetString("refuse-ert-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as ICommonSession;

            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var stationUid = EntitySystem.Get<StationSystem>().GetOwningStation(player.AttachedEntity.Value);

            if (stationUid == null)
            {
                shell.WriteLine(Loc.GetString("ert-command-invalid-grid"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetComponent<ERTCallComponent>(stationUid, out var component))
            {
                shell.WriteLine(Loc.GetString("ert-command-no-component"));
                return;
            }

            if (component.ERTCalled == false)
            {
                shell.WriteLine(Loc.GetString("ert-command-no-ert-called"));
                return;
            }

            if (component.ERTCalledTeam == null)
            {
                shell.WriteLine(Loc.GetString("ert-command-no-ert-called-in-component"));
                return;
            }

            EntitySystem.Get<ERTCallSystem>().Refuse(component, player.Name);
        }
    }
}
