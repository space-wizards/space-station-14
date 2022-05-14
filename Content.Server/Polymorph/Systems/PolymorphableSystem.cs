using Content.Server.Actions;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Polymorph;
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
            SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(AfterPolymorphAction);
        }

        private void AfterPolymorphAction(EntityUid uid, PolymorphableComponent component, PolymorphActionEvent args)
        {
            var child = Spawn(args.Prototype.Entity, Transform(uid).Coordinates);

            if (!TryComp<MindComponent>(uid, out var mind))
                return;

            if (mind.Mind == null)
                return;

            MakeSentientCommand.MakeSentient(child, _entity);

            var comp = AddComp<PolymorphedEntityComponent>(child);
            comp.Parent = uid;
            comp.Prototype = args.Prototype;

            //TODO: remove the container system altogether
            comp.ParentContainer.Insert(uid);
            RaiseLocalEvent(child, new AfterPolymorphEvent());
            mind.Mind.TransferTo(child);
        }

        public void CreatePolymorphAction(string id, EntityUid target)
        {
            if (!_proto.TryIndex<PolymorphPrototype>(id, out var polyproto))
                return;

            if (!_proto.TryIndex<EntityPrototype>(polyproto.Entity, out var entproto))
                return;

            var act = new InstantAction()
            {
                Event = new PolymorphActionEvent(polyproto),
                Name = Loc.GetString("polymorph-self-action-name", ("target", entproto.Name)),
                Description = Loc.GetString("polymorph-self-action-description", ("target", entproto.Name)),
            };

            _actions.AddAction(target, act, target);
        }

        /*
        public void RemovePolyMorphAction(PolymorphableComponent component, string prototypeId, bool forced)
        {
            if (!_proto.TryIndex<EntityPrototype>(prototypeId, out var prototype))
                return;

            var act = new InstantAction()
            {
                Event = new PolymorphActionEvent(prototypeId, forced),
                Name = Loc.GetString("polymorph-self-action-name", ("target", prototype.Name)),
                Description = Loc.GetString("polymorph-self-action-description", ("target", prototype.Name)),
            };
            
            foreach(var action in component.PolymorphActions)
            {
                if(action == act)
                {
                    _actions.RemoveAction(component.Owner, action);
                    return;
                }
            }    
        }*/

        private void OnStartup(EntityUid uid, PolymorphableComponent component, ComponentStartup args)
        {
            CreatePolymorphAction("mouse", component.Owner); //remove
            CreatePolymorphAction("carp", component.Owner); //remove
            //RemovePolyMorphAction(component, "MobCarp", false); //remove
        }
    }

    /// <summary>
    /// This event is used to initialize the info in polymorphedEntityComponent
    /// once all the information has been sent to it.
    /// </summary>
    public sealed class AfterPolymorphEvent : EventArgs { }

    public sealed class PolymorphActionEvent : InstantActionEvent
    {
        /// <summary>
        /// The polymorph prototype containing all the information about
        /// the specific polymorph.
        /// </summary>
        public readonly PolymorphPrototype Prototype;

        public PolymorphActionEvent(PolymorphPrototype prototype)
        {
            Prototype = prototype;
        }
    };
}
