using Content.Server.Actions;
using Content.Server.Body.Components;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems
{
    public sealed class TransformationChildSystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly IEntityManager _entity = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransformationChildComponent, ComponentInit>(OnComponentInit);

            SubscribeLocalEvent<TransformationChildComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<TransformationChildComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<TransformationChildComponent, RevertTransformationActionEvent>(Revert);
        }

        public void Revert(EntityUid uid, TransformationChildComponent component, RevertTransformationActionEvent args)
        {
            for(int i = 0; i < component.ParentContainer.ContainedEntities.Count; i++)
            {
                var entity = component.ParentContainer.ContainedEntities[i];
                component.ParentContainer.Remove(entity);
                if(entity == component.Parent)
                {
                    if (TryComp<MindComponent>(uid, out var mind))
                    {
                        if (mind.Mind != null)
                        {
                            mind.Mind.TransferTo(entity);
                        }
                    }
                }
            }
            QueueDel(uid);
        }

        private void OnStartup(EntityUid uid, TransformationChildComponent component, ComponentStartup args)
        {
            if (component.Action == null
            && _proto.TryIndex(component.ActionId, out InstantActionPrototype? act))
            {
                component.Action = new(act);
            }

            if (component.Action != null)
                _actions.AddAction(uid, component.Action, null);
        }

        private void OnShutdown(EntityUid uid, TransformationChildComponent component, ComponentShutdown args)
        {
            if (component.Action != null)
                _actions.RemoveAction(uid, component.Action);
        }

        private void OnComponentInit(EntityUid uid, TransformationChildComponent component, ComponentInit args)
        {
            component.ParentContainer = _container.EnsureContainer<Container>(uid, component.Name);
        }
    }
}

public sealed class RevertTransformationActionEvent : InstantActionEvent { };
