generator-clogged = {CAPITALIZE(THE($generator))} shuts off abruptly!

portable-generator-verb-start = Start generator
portable-generator-verb-start-msg-unreliable = Start the generator. This may take a few tries.
portable-generator-verb-start-msg-reliable = Start the generator.
portable-generator-verb-start-msg-unanchored = The generator must be anchored first!
portable-generator-verb-stop = Stop generator
portable-generator-start-fail = You tug the cord, but it didn't start.
portable-generator-start-success = You tug the cord, and it whirrs to life.

portable-generator-ui-title = Portable Generator
portable-generator-ui-status-stopped = Stopped:
portable-generator-ui-status-starting = Starting:
portable-generator-ui-status-running = Running:
portable-generator-ui-start = Start
portable-generator-ui-stop = Stop
portable-generator-ui-target-power-label = Target Power (kW):
portable-generator-ui-efficiency-label = Efficiency:
portable-generator-ui-fuel-use-label = Fuel use:
portable-generator-ui-fuel-left-label = Fuel left:
portable-generator-ui-clogged = Contaminants detected in fuel tank!
portable-generator-ui-eject = Eject
portable-generator-ui-eta = (~{ $minutes } min)
portable-generator-ui-unanchored = Unanchored
portable-generator-ui-current-output = Current output: {$voltage}
portable-generator-ui-network-stats = Network:
portable-generator-ui-network-stats-value = { POWERWATTS($supply) } / { POWERWATTS($load) }
portable-generator-ui-network-stats-not-connected = Not connected

power-switchable-generator-examine = The power output is set to {$voltage}.
power-switchable-generator-switched = Switched output to {$voltage}!

power-switchable-voltage = { $voltage ->
    [HV] [color=orange]HV[/color]
    [MV] [color=yellow]MV[/color]
    *[LV] [color=green]LV[/color]
}
power-switchable-switch-voltage = Switch to {$voltage}

fuel-generator-verb-disable-on = Turn the generator off first!
