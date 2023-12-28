// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed partial class GarbageClearUpCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public string Command => "ClearUpGarbage";
        public string Description => "Removes all objects with a tag 'trash' from the map";
        public string Help => "Surgery tommorow";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var _containerSystem = _entMan.System<SharedContainerSystem>();
            int cnt = 0;
            foreach (var ent in _entMan.GetEntities())
            {
                if (!_entMan.TryGetComponent<TagComponent>(ent, out var component))
                    continue;
                if (!component.Tags.Contains("Trash") || _containerSystem.IsEntityOrParentInContainer(ent))
                    continue;

                _entMan.DeleteEntity(ent);
                cnt++;
            }
            shell.WriteLine($"Карта очищена от {cnt} объектов мусора!");
        }
    }
}