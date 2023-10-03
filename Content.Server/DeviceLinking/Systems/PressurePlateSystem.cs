using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Interaction;
using Robust.Shared.Utility;
using Robust.Shared.Physics.Components;
using Content.Shared.Inventory;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Server.Storage.Components;

using Robust.Shared.Physics.Events;
using Content.Shared.StepTrigger.Systems;

namespace Content.Server.DeviceLinking.Systems
{
    public sealed class PressurePlateSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PressurePlateComponent, ComponentInit>(OnInit);
            //SubscribeLocalEvent<PressurePlateComponent, StepTriggeredEvent>(HandleTriggered, after: new[] { typeof(EntityStorageSystem)});
            SubscribeLocalEvent<PressurePlateComponent, StepTriggerAttemptEvent>(HandleTriggerAttempt);
            SubscribeLocalEvent<PressurePlateComponent, EndCollideEvent>(OnEndCollide);
            SubscribeLocalEvent<PressurePlateComponent, StartCollideEvent>(OnStartCollide);
        }

        private void OnInit(EntityUid uid, PressurePlateComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSourcePorts(uid, component.PressedSignal, component.ReleasedSignal);
        }

        private void HandleTriggerAttempt(EntityUid uid, PressurePlateComponent component, ref StepTriggerAttemptEvent args)
        {
            args.Continue = true;
        }
        private void OnStartCollide(EntityUid uid, PressurePlateComponent component, ref StartCollideEvent args)
        {
            var otherUid = args.OtherEntity;

            if (!args.OtherFixture.Hard)
                return;

            if (component.Colliding.Add(otherUid))
            {
                UpdateState(uid, component);
                Dirty(uid, component);
            }

        }

        private void OnEndCollide(EntityUid uid, PressurePlateComponent component, ref EndCollideEvent args)
        {
            var otherUid = args.OtherEntity;
            if (!component.Colliding.Remove(otherUid))
            {
                return;
            }
            UpdateState(uid, component);
            Dirty(uid, component);
        }

        private void UpdateState(EntityUid uid, PressurePlateComponent component)
        {
            var totalMass = 0f;
            foreach (var ent in component.Colliding)
            {
                if (Deleted(ent))
                {
                    component.Colliding.Remove(ent);
                    continue;
                }
                totalMass += GetEntWeightRecursive(ent);
            }
            Log.Debug("Total Mass: " + totalMass);

            if (component.IsPressed && totalMass < component.WeightRequired) //Release
            {
                component.IsPressed = false;
                _signalSystem.InvokePort(uid, component.ReleasedSignal);
            }
            if (!component.IsPressed && totalMass > component.WeightRequired) //Press
            {
                component.IsPressed = true;
                _signalSystem.InvokePort(uid, component.PressedSignal);
            }
        }

        /// <summary>
        /// Recursively calculates the weight of the object, and all its contents, and the contents and its contents...
        /// </summary>
        private float GetEntWeightRecursive(EntityUid uid)
        {
            var totalMass = 0f;
            if (Deleted(uid)) return 0f;
            if (TryComp(uid, out PhysicsComponent? physics))
            {
                totalMass += physics.Mass;
            }
            if (TryComp(uid, out EntityStorageComponent? entityStorage))
            {
                var storage = entityStorage.Contents;
                foreach (var ent in storage.ContainedEntities)
                {
                    totalMass += GetEntWeightRecursive(ent);
                }
            }
            return totalMass;
        }
    }
}
