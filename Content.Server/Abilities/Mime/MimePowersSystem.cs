using System;
using Content.Server.Popups;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Abilities.Mime
{
    public sealed class MimePowersSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MimePowersComponent, SpeakAttemptEvent>(OnSpeakAttempt);
            SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var invisWall in EntityQuery<InvisibleWallComponent>())
            {
                invisWall.Accumulator += frameTime;
                if (invisWall.Accumulator < invisWall.DespawnTime)
                {
                    continue;
                }
                EntityManager.QueueDeleteEntity(invisWall.Owner);
            }
        }

        private void OnComponentInit(EntityUid uid, MimePowersComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.InvisibleWallAction, uid);
            _alertsSystem.ShowAlert(uid, AlertType.VowOfSilence);
        }
        private void OnSpeakAttempt(EntityUid uid, MimePowersComponent component, SpeakAttemptEvent args)
        {
            if (!component.Enabled)
                return;

            _popupSystem.PopupEntity(Loc.GetString("mime-cant-speak"), uid, Filter.Entities(uid));
            args.Cancel();
        }

        /// <summary>
        /// Creates an invisible wall in a free space after some checks.
        /// </summary>
        private void OnInvisibleWall(EntityUid uid, MimePowersComponent component, InvisibleWallActionEvent args)
        {
            if (!component.Enabled)
                return;
            var xform = Transform(uid);
            /// Get the tile in front of the mime
            var offsetValue = xform.LocalRotation.ToWorldVec().Normalized;
            var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid();
            /// Check there are no walls or mobs there
            foreach (var entity in coords.GetEntitiesInTile())
            {
                if (_tagSystem.HasTag(entity, "Wall") ||(HasComp<MobStateComponent>(entity) && entity != uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), uid, Filter.Entities(uid));
                    return;
                }
            }
            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-popup", ("mime", uid)), uid, Filter.Pvs(uid));
            /// Make sure we set the invisible wall to despawn properly
            var wall = EntityManager.SpawnEntity(component.WallPrototype, coords);
            var invisWall = EnsureComp<InvisibleWallComponent>(wall);
            /// Handle args so cooldown words
            args.Handled = true;
        }
    }

    public sealed class InvisibleWallActionEvent : InstantActionEvent {}
}
