#nullable enable
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Body.Part;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Server.Body.Part
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    public class BodyPartComponent : SharedBodyPartComponent
    {
        private Container _mechanismContainer = default!;

        public override bool CanAddMechanism(SharedMechanismComponent mechanism)
        {
            return base.CanAddMechanism(mechanism) &&
                   _mechanismContainer.CanInsert(mechanism.Owner);
        }

        protected override void OnAddMechanism(SharedMechanismComponent mechanism)
        {
            base.OnAddMechanism(mechanism);

            _mechanismContainer.Insert(mechanism.Owner);
        }

        protected override void OnRemoveMechanism(SharedMechanismComponent mechanism)
        {
            base.OnRemoveMechanism(mechanism);

            _mechanismContainer.Remove(mechanism.Owner);
            mechanism.Owner.RandomOffset(0.25f);
        }

        public override void Initialize()
        {
            base.Initialize();

            _mechanismContainer = Owner.EnsureContainer<Container>($"{Name}-{nameof(BodyPartComponent)}");

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var mechanismId in MechanismIds)
            {
                var entity = Owner.EntityManager.SpawnEntity(mechanismId, Owner.Transform.MapPosition);

                if (!entity.TryGetComponent(out SharedMechanismComponent? mechanism))
                {
                    Logger.Error($"Entity {mechanismId} does not have a {nameof(SharedMechanismComponent)} component.");
                    continue;
                }

                TryAddMechanism(mechanism, true);
            }
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var mechanism in Mechanisms)
            {
                mechanism.Dirty();
            }
        }

        [Verb]
        public class AttachBodyPartVerb : Verb<BodyPartComponent>
        {
            protected override void GetData(IEntity user, BodyPartComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (user == component.Owner)
                {
                    return;
                }

                if (!user.TryGetComponent(out ActorComponent? actor))
                {
                    return;
                }

                var groupController = IoCManager.Resolve<IConGroupController>();

                if (!groupController.CanCommand(actor.PlayerSession, "attachbodypart"))
                {
                    return;
                }

                if (!user.TryGetComponent(out SharedBodyComponent? body))
                {
                    return;
                }

                if (body.HasPart(component))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Attach Body Part");
            }

            protected override void Activate(IEntity user, BodyPartComponent component)
            {
                if (!user.TryGetComponent(out SharedBodyComponent? body))
                {
                    return;
                }

                body.SetPart($"{nameof(AttachBodyPartVerb)}-{component.Owner.Uid}", component);
            }
        }
    }
}
