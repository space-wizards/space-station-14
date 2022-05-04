using Content.Server.Actions;
using Content.Server.Body.Components;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems
{
    public sealed class TransformationParentSystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly IEntityManager _entity = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransformationParentComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<TransformationParentComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<TransformationParentComponent, TransformationActionEvent>(Transform);
        }

        public void Transform(EntityUid uid, TransformationParentComponent component, TransformationActionEvent args)
        {
            var child = Spawn(component.TransformPrototype, Transform(uid).Coordinates);

            if (TryComp<MindComponent>(uid, out var mind))
            {
                if (mind.Mind != null)
                {
                    MakeSentientCommand.MakeSentient(child, _entity);
                    mind.Mind.TransferTo(child);

                    var comp = AddComp<TransformationChildComponent>(child);
                    comp.Parent = uid;
                    comp.ParentContainer.Insert(uid);
                }
            }
        }

        private void OnStartup(EntityUid uid, TransformationParentComponent component, ComponentStartup args)
        {
            if (component.TransformAction == null
            && _proto.TryIndex(component.ActionId, out InstantActionPrototype? act))
            {
                component.TransformAction = new(act);
            }

            if (component.TransformAction != null)
                _actions.AddAction(uid, component.TransformAction, null);
        }

        private void OnShutdown(EntityUid uid, TransformationParentComponent component, ComponentShutdown args)
        {
            if (component.TransformAction != null)
                _actions.RemoveAction(uid, component.TransformAction);
        }
    }
}
public sealed class TransformationActionEvent : InstantActionEvent { };
