using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Server.StationEvents;
using Content.Shared;
using Content.Shared.GameTicking;
using Content.Shared.Network.NetMessages;
using JetBrains.Annotations;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    // Somewhat based off of TG's implementation of events
    public sealed class StationEventSystem : EntitySystem, IResettingEntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IConGroupController _conGroupController = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public StationEvent CurrentEvent { get; private set; }
        public IReadOnlyCollection<StationEvent> StationEvents => _stationEvents;

        private readonly List<StationEvent> _stationEvents = new();

        private const float MinimumTimeUntilFirstEvent = 300;

        /// <summary>
        /// How long until the next check for an event runs
        /// </summary>
        /// Default value is how long until first event is allowed
        private float _timeUntilNextEvent = MinimumTimeUntilFirstEvent;

        /// <summary>
        /// Whether random events can run
        /// </summary>
        /// If disabled while an event is running (even if admin run) it will disable it
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                CurrentEvent?.Shutdown();
                CurrentEvent = null;
            }
        }

        private bool _enabled = true;

        /// <summary>
        /// Admins can get a list of all events available to run, regardless of whether their requirements have been met
        /// </summary>
        /// <returns></returns>
        public string GetEventNames()
        {
            StringBuilder result = new StringBuilder();

            foreach (var stationEvent in _stationEvents)
            {
                result.Append(stationEvent.Name + "\n");
            }

            return result.ToString();
        }

        /// <summary>
        /// Admins can forcibly run events by passing in the Name
        /// </summary>
        /// <param name="name">The exact string for Name, without localization</param>
        /// <returns></returns>
        public string RunEvent(string name)
        {
            // Could use a dictionary but it's such a minor thing, eh.
            // Wasn't sure on whether to localize this given it's a command
            var upperName = name.ToUpperInvariant();

            foreach (var stationEvent in _stationEvents)
            {
                if (stationEvent.Name.ToUpperInvariant() != upperName)
                {
                    continue;
                }

                CurrentEvent?.Shutdown();
                CurrentEvent = stationEvent;
                stationEvent.Startup();
                return Loc.GetString("Running event ") + stationEvent.Name;
            }

            // I had string interpolation but lord it made it hard to read
            return Loc.GetString("No event named ") + name;
        }

        /// <summary>
        /// Randomly run a valid event immediately, ignoring earlieststart
        /// </summary>
        /// <returns></returns>
        public string RunRandomEvent()
        {
            var availableEvents = AvailableEvents(true);
            var randomEvent = FindEvent(availableEvents);

            if (randomEvent == null)
            {
                return Loc.GetString("No valid events available");
            }

            CurrentEvent?.Shutdown();
            CurrentEvent = randomEvent;
            CurrentEvent.Startup();

            return Loc.GetString("Running ") + randomEvent.Name;
        }

        /// <summary>
        /// Admins can stop the currently running event (if applicable) and reset the timer
        /// </summary>
        /// <returns></returns>
        public string StopEvent()
        {
            string resultText;

            if (CurrentEvent == null)
            {
                resultText = Loc.GetString("No event running currently");
            }
            else
            {
                resultText = Loc.GetString("Stopped event ") + CurrentEvent.Name;
                CurrentEvent.Shutdown();
                CurrentEvent = null;
            }

            ResetTimer();
            return resultText;
        }

        public override void Initialize()
        {
            base.Initialize();
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (var type in reflectionManager.GetAllChildren(typeof(StationEvent)))
            {
                if (type.IsAbstract) continue;

                var stationEvent = (StationEvent) typeFactory.CreateInstance(type);
                _stationEvents.Add(stationEvent);
            }

            // Can't just check debug / release for a default given mappers need to use release mode
            // As such we'll always pause it by default.
            _configurationManager.OnValueChanged(CCVars.EventsEnabled, value => Enabled = value, true);

            _netManager.RegisterNetMessage<MsgRequestStationEvents>(nameof(MsgRequestStationEvents), RxRequest);
            _netManager.RegisterNetMessage<MsgStationEvents>(nameof(MsgStationEvents));
        }

        private void RxRequest(MsgRequestStationEvents msg)
        {
            if (_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var player))
                SendEvents(player);
        }

        private void SendEvents(IPlayerSession player)
        {
            if (!_conGroupController.CanCommand(player, "events"))
                return;

            var newMsg = _netManager.CreateNetMessage<MsgStationEvents>();
            newMsg.Events = StationEvents.Select(e => e.Name).ToArray();
            _netManager.ServerSendMessage(newMsg, player.ConnectedClient);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Enabled)
            {
                return;
            }

            // Stop events from happening in lobby and force active event to end if the round ends
            if (_gameTicker.RunLevel != GameRunLevel.InRound)
            {
                if (CurrentEvent != null)
                {
                    Enabled = false;
                }

                return;
            }

            // Keep running the current event
            if (CurrentEvent != null)
            {
                CurrentEvent.Update(frameTime);

                // Shutdown the event and set the timer for the next event
                if (!CurrentEvent.Running)
                {
                    CurrentEvent.Shutdown();
                    CurrentEvent = null;
                    ResetTimer();
                }

                return;
            }

            if (_timeUntilNextEvent > 0)
            {
                _timeUntilNextEvent -= frameTime;
                return;
            }

            // No point hammering this trying to find events if none are available
            var stationEvent = FindEvent(AvailableEvents());
            if (stationEvent == null)
            {
                ResetTimer();
            }
            else
            {
                CurrentEvent = stationEvent;
                CurrentEvent.Startup();
            }
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer()
        {
            // 5 - 15 minutes. TG does 3-10 but that's pretty frequent
            _timeUntilNextEvent = _random.Next(300, 900);
        }

        /// <summary>
        /// Pick a random event from the available events at this time, also considering their weightings.
        /// </summary>
        /// <returns></returns>
        private StationEvent FindEvent(List<StationEvent> availableEvents)
        {
            if (availableEvents.Count == 0)
            {
                return null;
            }

            var sumOfWeights = 0;

            foreach (var stationEvent in availableEvents)
            {
                sumOfWeights += (int) stationEvent.Weight;
            }

            sumOfWeights = _random.Next(sumOfWeights);

            foreach (var stationEvent in availableEvents)
            {
                sumOfWeights -= (int) stationEvent.Weight;

                if (sumOfWeights <= 0)
                {
                    return stationEvent;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the events that have met their player count, time-until start, etc.
        /// </summary>
        /// <param name="ignoreEarliestStart"></param>
        /// <returns></returns>
        private List<StationEvent> AvailableEvents(bool ignoreEarliestStart = false)
        {
            TimeSpan currentTime;
            var playerCount = _playerManager.PlayerCount;

            // playerCount does a lock so we'll just keep the variable here
            if (!ignoreEarliestStart)
            {
                currentTime = _gameTiming.CurTime;
            }
            else
            {
                currentTime = TimeSpan.Zero;
            }

            var result = new List<StationEvent>();

            foreach (var stationEvent in _stationEvents)
            {
                if (CanRun(stationEvent, playerCount, currentTime))
                {
                    result.Add(stationEvent);
                }
            }

            return result;
        }

        private bool CanRun(StationEvent stationEvent, int playerCount, TimeSpan currentTime)
        {
            if (stationEvent.MaxOccurrences.HasValue && stationEvent.Occurrences >= stationEvent.MaxOccurrences.Value)
            {
                return false;
            }

            if (playerCount < stationEvent.MinimumPlayers)
            {
                return false;
            }

            if (currentTime != TimeSpan.Zero && currentTime.TotalMinutes < stationEvent.EarliestStart)
            {
                return false;
            }

            return true;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CurrentEvent?.Shutdown();
        }

        public void Reset()
        {
            if (CurrentEvent != null && CurrentEvent.Running)
            {
                CurrentEvent.Shutdown();
                CurrentEvent = null;
            }

            foreach (var stationEvent in _stationEvents)
            {
                stationEvent.Occurrences = 0;
            }

            _timeUntilNextEvent = MinimumTimeUntilFirstEvent;
        }
    }
}
