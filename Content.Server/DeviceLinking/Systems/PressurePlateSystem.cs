using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Physics.Components;
using Content.Server.Storage.Components;

using Robust.Shared.Physics.Events;
using Content.Shared.StepTrigger.Systems;

namespace Content.Server.DeviceLinking.Systems
{
    public sealed class PressurePlateSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PressurePlateComponent, ComponentInit>(OnInit);
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
            if (component.State == PressurePlateState.Pressed && totalMass < component.WeightRequired) //Release
            {
                component.State = PressurePlateState.Released;
                _signalSystem.InvokePort(uid, component.ReleasedSignal);

                _audio.PlayPvs(component.ReleasedSound, uid);
            }
            if (component.State == PressurePlateState.Released && totalMass > component.WeightRequired) //Press
            {
                component.State = PressurePlateState.Pressed;
                _signalSystem.InvokePort(uid, component.PressedSignal);
                _audio.PlayPvs(component.PressedSound, uid);
            }

            if (TryComp(uid, out AppearanceComponent? appearance))
                _appearance.SetData(uid, PressurePlateVisuals.State, component.State, appearance);

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
