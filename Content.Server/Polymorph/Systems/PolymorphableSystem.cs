using Content.Server.Actions;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Systems
{
    public sealed class PolymorphableSystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly IEntityManager _entity = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(Transform);
        }

        public void Transform(EntityUid uid, PolymorphableComponent component, PolymorphActionEvent args)
        {
            var child = Spawn(args.Prototype, Transform(uid).Coordinates);

            if (!TryComp<MindComponent>(uid, out var mind))
                return;

            if (mind.Mind == null)
                return;

            MakeSentientCommand.MakeSentient(child, _entity);

            var comp = AddComp<PolymorphedEntityComponent>(child);
            comp.Parent = uid;
            comp.Forced = args.Forced;

            comp.ParentContainer.Insert(uid);
            RaiseLocalEvent(child, new AfterPolymorphEvent());
            mind.Mind.TransferTo(child);
        }

        public void CreateTransformationAction(PolymorphableComponent component, string prototypeId, bool forced)
        {
            var act = new InstantAction();

            act.Event = new PolymorphActionEvent(prototypeId, forced);
            act.Name = "badda bing" + prototypeId;
            act.Description = "badda boom" + prototypeId;

            _actions.AddAction(component.Owner, act, component.Owner);
        }

        private void OnStartup(EntityUid uid, PolymorphableComponent component, ComponentStartup args)
        {
            CreateTransformationAction(component, "MobMouse", false);
            CreateTransformationAction(component, "MobCarp", true);
        }
    }
}

/// <summary>
/// This event is used to initialize the event in the polymorphedEntityComponent
/// once all the information has been sent to it.
/// </summary>
public sealed class AfterPolymorphEvent : EventArgs { }

public sealed class PolymorphActionEvent : InstantActionEvent
{
    /// <summary>
    /// The prototype Id of the entity that the target will be polymorphed into
    /// </summary>
    public readonly string Prototype;

    /// <summary>
    /// Whether or not the transformation is happening at will and if it can be reversed at will.
    /// </summary>
    public readonly bool Forced;

    public PolymorphActionEvent(string prototype, bool forced)
    {
        Prototype = prototype;
        Forced = forced;
    }
};
