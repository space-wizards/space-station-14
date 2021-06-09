### Commands

## AI

faction-command-description = Update / list factional relationships for NPCs.
faction-command-help-text = faction <source> <friendly/hostile> target
                            faction <source> list: hostile factions
faction-command-invalid-faction-error = Invalid faction
faction-command-invalid-target-faction-error = Invalid target faction
faction-command-no-target-faction-error = Need to supply a target faction
faction-command-unknown-faction-argument-error = Unknown faction argument

## Chat

suicide-command-description = Commits suicide
suicide-command-help-text = The suicide command gives you a quick way out of a round while remaining in-character.
                            The method varies, first it will attempt to use the held item in your active hand.
                            If that fails, it will attempt to use an object in the environment.
                            Finally, if neither of the above worked, you will die by biting your tongue.
suicide-command-default-text-others = {$name} is attempting to bite their own tongue!
suicide-command-default-text-self = You attempt to bite your own tongue!

## Disposal

tube-connections-command-description = Shows all the directions that a tube can connect in.
tube-connections-command-help-text = Usage: {$command} <entityUid>

## Station Event

station-event-command-description = Provides admin control to station events
station-event-command-help-text = events <running/list/pause/resume/stop/run <eventName/random>>
                                  running: return the current running event
                                  list: return all event names that can be run
                                  pause: stop all random events from running and any one currently running
                                  resume: allow random events to run again
                                  run <eventName/random>: start a particular event now; <eventName> is case-insensitive and not localized
station-event-command-running-help-text = running: return the current running event
station-event-command-list-help-text = list: return all event names that can be run
station-event-command-pause-help-text = pause: stop all random events from running and any one currently running
station-event-command-resume-help-text = resume: allow random events to run again
station-event-command-run-help-text = run <eventName/random>: start a particular event now; <eventName> is case-insensitive and not localized
station-event-command-no-event-running-message = No station event running
station-event-command-event-list = Random
                                   {$otherEvents}
station-event-command-events-paused-message = Station events paused
station-event-command-events-already-paused-message = Station events are already paused
station-event-command-events-resumed-message = Station events resumed
station-event-command-events-already-running-message = Station events are already running
