using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Singularity.Components;
using Robust.Shared.Utility;


namespace Content.Server.Singularity.EntitySystems
{
    public sealed class ContainmentAlarmSystem : EntitySystem
    {
        [Dependency] private readonly NavMapSystem _navMap = default!;
        [Dependency] private readonly RadioSystem _radio = default!;

        /// <summary>
        /// Resets the alarm to it's base state
        /// </summary>
        public void ResetAlarm(EntityUid uid, ContainmentAlarmComponent alarm)
        {
            alarm.LastAlertPowerLevel = -1;
        }

        /// <summary>
        /// Given the current power of the generator and what power it was at last time this alarm went off, sends an alert accross the radio with current power levels.
        /// Channel is seperated into AnnouncementChannel and EmergencyAnnouncementChannel, the latter only kicking in at under the EmergencyThreshold.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="alarm"></param>
        /// <param name="currentPower"></param>
        public void UpdateAlertLevel(Entity<ContainmentAlarmComponent?> ent, int currentPower)
        {
            if (!Resolve(ent, ref ent.Comp))
                return;

            var alarm = ent.Comp;

            if(alarm.LastAlertPowerLevel == -1 || alarm.LastAlertPowerLevel - currentPower >= alarm.PowerIntervalBetweenAlerts)
            {
                //Alert territory
                var posText = FormattedMessage.RemoveMarkup(_navMap.GetNearestBeaconString(ent.Owner));
                var channel = currentPower <= alarm.EmergencyThreshold ? alarm.EmergencyAnnouncementChannel : alarm.AnnouncementChannel;
                var powerText = (double)currentPower / alarm.PowerCap;
                var message = Loc.GetString("comp-containment-alert-field-losing-power", ("location", posText), ("power", powerText));
                _radio.SendRadioMessage(ent, message, channel, ent, escapeMarkup: false);
                alarm.LastAlertPowerLevel = currentPower;
            }
        }
        /// <summary>
        /// Sends over the emergency radio the location of where this containment field broke
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="alarm"></param>

        public void BroadcastContainmentBreak(Entity<ContainmentAlarmComponent?> ent)
        {
            if (!Resolve(ent, ref ent.Comp))
                return;

            var alarm = ent.Comp;

            var posText = FormattedMessage.RemoveMarkup(_navMap.GetNearestBeaconString(ent.Owner));
            var message = Loc.GetString("comp-containment-alert-field-link-broken", ("location", posText));
            _radio.SendRadioMessage(ent, message, alarm.EmergencyAnnouncementChannel, ent, escapeMarkup: false);
        }
    }
}
