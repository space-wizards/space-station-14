### Localization for events console commands

## 'events' command
cmd-events-desc = Provides admin control to station events
cmd-events-help = events <running/list/pause/resume/stop/run <eventName/random>>
                  running: return the current running event
                  list: return all event names that can be run
                  pause: stop all random events from running and any one currently running
                  resume: allow random events to run again
                  run <eventName/random>: start a particular event now; <eventName> is case-insensitive and not localized
cmd-events-arg-subcommand = <subcommand>
cmd-events-arg-run-eventName = <eventName>

cmd-events-none-running = No station event running
cmd-events-list-random = Random
cmd-events-paused = Station events paused
cmd-events-already-paused = Station events are already paused
cmd-events-resumed = Station events resumed
cmd-events-already-running = Station events are already running
