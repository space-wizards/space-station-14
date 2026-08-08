# Commands
## Delay shuttle round end
cmd-delayroundend-desc = Pauses or adjusts the timer until the round ends, or until it restarts after the manifest.
cmd-delayroundend-help =
    Usage: delayroundend [seconds]
    Positive values add time and negative values subtract it. With no argument, pauses or resumes the timer.
cmd-delayroundend-invalid-seconds = {$value} isn't a valid amount of seconds.
cmd-delayroundend-paused =
    { $restart ->
        [true] Round restart paused.
       *[false] Round end paused.
    }
cmd-delayroundend-resumed =
    { $restart ->
        [true] Round restart resumed.
       *[false] Round end resumed.
    }
cmd-delayroundend-extended =
    { $restart ->
        [true] Round restart delayed by {$seconds} seconds.
       *[false] Round end extended by {$seconds} seconds.
    }
cmd-delayroundend-shortened =
    { $restart ->
        [true] Round restart moved forward by {$seconds} seconds.
       *[false] Round end shortened by {$seconds} seconds.
    }
cmd-delayroundend-no-timer = There is no round end or restart timer to adjust.

## Dock emergency shuttle
cmd-dockemergencyshuttle-desc = Calls the emergency shuttle and docks it to the station... if it can.
cmd-dockemergencyshuttle-help = Usage: dockemergencyshuttle

## Launch emergency shuttle
cmd-launchemergencyshuttle-desc = Early launches the emergency shuttle if possible.
cmd-launchemergencyshuttle-help = Usage: launchemergencyshuttle

# Emergency shuttle
emergency-shuttle-left = The Emergency Shuttle has left the station. Estimate {$transitTime} seconds until the shuttle arrives at CentComm.
emergency-shuttle-launch-time = The emergency shuttle will launch in {$consoleAccumulator} seconds.
emergency-shuttle-docked = The Emergency Shuttle has docked {$direction} of the station, {$location}. It will leave in {$time} seconds.{$extended}
emergency-shuttle-good-luck = The Emergency Shuttle is unable to find a station. Good luck.
emergency-shuttle-nearby = The Emergency Shuttle is unable to find a valid docking port. It has warped in {$direction} of the station, {$location}. It will leave in {$time} seconds.{$extended}
emergency-shuttle-extended = {" "}Launch time has been extended due to inconvenient circumstances.

# Emergency shuttle console popup / announcement
emergency-shuttle-console-no-early-launches = Early launch is disabled
emergency-shuttle-console-auth-left = {$remaining} authorizations needed until shuttle is launched early.
emergency-shuttle-console-auth-revoked = Early launch authorization revoked, {$remaining} authorizations needed.
emergency-shuttle-console-denied = Access denied

# UI
emergency-shuttle-console-window-title = Emergency Shuttle Console
emergency-shuttle-ui-engines = ENGINES:
emergency-shuttle-ui-idle = Idle
emergency-shuttle-ui-status-title = SHUTTLE STATUS
emergency-shuttle-ui-status-waiting = Awaiting launch
emergency-shuttle-ui-status-early-disabled = Standard route
emergency-shuttle-ui-status-countdown = Launch in {$time}
emergency-shuttle-ui-route = Route:
emergency-shuttle-ui-route-standard = Central Command
emergency-shuttle-ui-route-raider-outpost = Syndicate Raider Outpost
emergency-shuttle-ui-repeal-all = Repeal All
emergency-shuttle-ui-early-authorize = Early Launch Authorization
emergency-shuttle-ui-authorize = AUTHORIZE
emergency-shuttle-ui-repeal = REPEAL
emergency-shuttle-ui-authorizations = Authorizations
emergency-shuttle-ui-remaining = Remaining: {$remaining}
emergency-shuttle-console-hijack-denied = No active hijack contract found.
emergency-shuttle-console-hijack-already-started = Hijack sequence is already active.
emergency-shuttle-console-hijack-already-complete = The shuttle is already compromised.
emergency-shuttle-console-hijack-announcer = Station Automated Systems
emergency-shuttle-console-hijack-started = Attention all facility officers. Unauthorized access to emergency shuttle systems has been detected. A bluespace route alteration is in progress; exact location unknown. Security is ordered to disable the rerouting protocols on the emergency shuttle bridge using the appropriate control console.
emergency-shuttle-console-hijack-cancelled = Attention, rerouting protocols have been cancelled. Evacuation continues under standard procedure.
emergency-shuttle-console-hijack-completed = Attention, the emergency shuttle bluespace route has been altered. Bluespace jump initiated. All crew are advised to leave the emergency shuttle.
emergency-shuttle-ui-hijack = BLUESPACE ROUTE REROUTE
emergency-shuttle-ui-hijack-start = INITIATE REROUTE
emergency-shuttle-ui-hijack-cancel = DISABLE PROTOCOLS
emergency-shuttle-ui-hijack-inactive = Rerouting protocols ready
emergency-shuttle-ui-hijack-active = Route alteration: {$time}
emergency-shuttle-ui-hijack-complete = Route altered
emergency-shuttle-ui-hijack-welcome = Welcome, {$name}
emergency-shuttle-ui-hijack-unknown = UNKNOWN

# Map Misc.
map-name-centcomm = Central Command
map-name-terminal = Arrivals Terminal
