using System;
using System.Collections.Generic;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Mobs
{
    /// <summary>
    /// Handles the icons on the right side of the screen.
    /// Should only be used for player-controlled entities
    /// </summary>
    public abstract class SharedAlertsComponent : Component
    {
        [Dependency]
        protected readonly AlertManager AlertManager = default!;

        public override string Name => "AlertsUI";
        public override uint? NetID => ContentNetIDs.ALERTS;

        public abstract IReadOnlyDictionary<AlertSlot, AlertState> Alerts { get; }

        /// <summary>
        /// Shows the alert. If the alert or another alert of the same category is already showing,
        /// it will be updated with the specified values.
        /// </summary>
        /// <param name="alertId">id of the alert to set</param>
        /// <param name="severity">severity, if supported by the alert</param>
        /// <param name="cooldown">cooldown start and end, if null there will be no cooldown (and it will
        /// be erased if there is currently a cooldown for the alert)</param>
        public abstract void ShowAlert(string alertId, short? severity = null, ValueTuple<TimeSpan, TimeSpan>? cooldown = null);

        // TODO: by category or by id
        public abstract void ClearAlert(AlertSlot effect);
    }

    [Serializable, NetSerializable]
    public class AlertsComponentState : ComponentState
    {
        // TODO: not sure a dict is really the best way to transmit this
        public Dictionary<AlertSlot, AlertState> Alerts;

        public AlertsComponentState(Dictionary<AlertSlot, AlertState> alerts) : base(ContentNetIDs.ALERTS)
        {
            Alerts = alerts;
        }
    }

    /// <summary>
    /// A message that calls the click interaction on a alert
    /// </summary>
    [Serializable, NetSerializable]
    public class ClickAlertMessage : ComponentMessage
    {
        public readonly AlertSlot Effect;

        public ClickAlertMessage(AlertSlot effect)
        {
            Directed = true;
            Effect = effect;
        }
    }

    [Serializable, NetSerializable]
    public struct AlertState
    {
        public int AlertEncoded;
        public short? Severity;
        public ValueTuple<TimeSpan, TimeSpan>? Cooldown;
    }

    public enum AlertSlot
    {
        Health,
        Hunger,
        Thirst,
        Pressure,
        Fire,
        Temperature,
        Stun,
        Cuffed,
        Buckled,
        Piloting,
        Pulling,
        Pulled,
        Weightless,
        Error
    }
}
