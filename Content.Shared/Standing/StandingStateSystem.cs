using Content.Shared.Hands.Components;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Standing
{
    public sealed class StandingStateSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        // If StandingCollisionLayer value is ever changed to more than one layer, the logic needs to be edited.
        private const int StandingCollisionLayer = (int) CollisionGroup.MidImpassable;

        public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            return !standingState.Standing;
        }

        public bool Down(EntityUid uid, bool playSound = true, bool dropHeldItems = true,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            HandsComponent? hands = null)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, ref hands, false);

            // This is just to avoid most callers doing this manually saving boilerplate
            // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
            // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
            // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
            if (dropHeldItems && hands != null)
            {
                RaiseLocalEvent(uid, new DropHandItemsEvent(), false);
            }

            var msg = new DownAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;

            standingState.Standing = false;
            Dirty(standingState);
            RaiseLocalEvent(uid, new DownedEvent(), false);

            // Seemed like the best place to put it
            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, appearance);

            ChangeCollision(uid, standingState, dropHeldItems);

            // check if component was just added or streamed to client
            // if true, no need to play sound - mob was down before player could seen that
            if (standingState.LifeStage <= ComponentLifeStage.Starting)
                return true;

            if (playSound)
            {
                _audio.PlayPredicted(standingState.DownSound, uid, uid);
            }

            return true;
        }

        public bool Stand(EntityUid uid,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            bool force = false)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, false);

            if (standingState.Standing)
                return true;

            if (!force)
            {
                var msg = new StandAttemptEvent();
                RaiseLocalEvent(uid, msg, false);

                if (msg.Cancelled)
                    return false;
            }

            standingState.Standing = true;
            Dirty(uid, standingState);
            RaiseLocalEvent(uid, new StoodEvent(), false);

            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical, appearance);

            RevertCollisionChange(uid, standingState);

            return true;
        }

        /// <summary>
        /// Change collision masks to allow going under certain entities like flaps and tables
        /// Also change layers so projectiles will pass over laying down entities.
        /// </summary>
        public void ChangeCollision(EntityUid uid,
            StandingStateComponent? standingState = null,
            bool dropHeldItems = true)
        {
            if (!Resolve(uid, ref standingState, false))
                return;

            var layingDownLayer = (int) standingState.LayingDownLayer;

            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var (key, fixture) in fixtureComponent.Fixtures)
                {
                    if ((fixture.CollisionLayer & (int)standingState.StandingLayer) != 0 && dropHeldItems)
                    {
                        standingState.LayerChangedFixtures.Add(key);

                        _physics.SetCollisionLayer(uid, key, fixture,
                            (fixture.CollisionLayer & ~(int) standingState.StandingLayer) | layingDownLayer,
                            manager: fixtureComponent);
                    }

                    if ((fixture.CollisionMask & StandingCollisionLayer) == 0)
                        continue;

                    standingState.MaskChangedFixtures.Add(key);
                    _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask & ~StandingCollisionLayer, manager: fixtureComponent);
                }
            }
        }

        /// <summary>
        /// Returns the entities collision mask and layer to their original values again.
        /// </summary>
        public void RevertCollisionChange(EntityUid uid, StandingStateComponent? standingState = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return;

            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var key in standingState.MaskChangedFixtures)
                {
                    if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                        _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask | StandingCollisionLayer, fixtureComponent);
                }

                foreach (var key in standingState.LayerChangedFixtures)
                {
                    if (!fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                        continue;

                    _physics.SetCollisionLayer(uid, key, fixture,
                        (fixture.CollisionLayer & ~(int) standingState.LayingDownLayer) | (int) standingState.StandingLayer,
                        manager: fixtureComponent);
                }
            }
            standingState.MaskChangedFixtures.Clear();
            standingState.LayerChangedFixtures.Clear();
        }
    }

    public sealed class DropHandItemsEvent : EventArgs
    {
    }

    /// <summary>
    /// Subscribe if you can potentially block a down attempt.
    /// </summary>
    public sealed class DownAttemptEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Subscribe if you can potentially block a stand attempt.
    /// </summary>
    public sealed class StandAttemptEvent : CancellableEntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity becomes standing
    /// </summary>
    public sealed class StoodEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity is not standing
    /// </summary>
    public sealed class DownedEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when the collision of an entity is attempted to be reverted.
    /// </summary>
    [ByRefEvent]
    public record struct TryRevertCollisionChangeEvent(bool Cancelled = false);
}
