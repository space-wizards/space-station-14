
using Content.Shared.Interaction.Components;
using Content.Server.Hands.Components;
using Content.Server.Light.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Actions;
using Robust.Shared.Random;

namespace Content.Server.Borgs
{
    public sealed class InnateToolSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InnateToolComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<InnateToolComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnInit(EntityUid uid, InnateToolComponent component, ComponentInit args)
        {
            if (component.Tools.Count == 0)
                return;

            var spawnCoord = Transform(uid).Coordinates;

            if (TryComp<HandsComponent>(uid, out var hands) && hands.Count >= component.Tools.Count)
            {
                var items = EntitySpawnCollection.GetSpawns(component.Tools, _robustRandom);
                foreach (var entry in items)
                {
                    var item = Spawn(entry, spawnCoord);
                    AddComp<UnremoveableComponent>(item);
                    if (!_handsSystem.TryPickupAnyHand(uid, item, checkActionBlocker: false))
                    {
                        QueueDel(item);
                        Logger.Error($"component ({ToPrettyString(uid)}) failed to pick up innate item ({ToPrettyString(item)})");
                        continue;
                    }
                    component.ToolUids.Add(item);
                }
            }

            if (TryComp<ActionsComponent>(uid, out var actions) && TryComp<UnpoweredFlashlightComponent>(uid, out var flashlight))
            {
                _actionsSystem.AddAction(uid, flashlight.ToggleAction, null, actions);
            }
        }

        private void OnShutdown(EntityUid uid, InnateToolComponent component, ComponentShutdown args)
        {
            foreach (var tool in component.ToolUids)
            {
                QueueDel(tool);
            }
        }
    }
}
