# Battery Status
battery-status-charge = Charge: [color=#5E7C16]{$percent}[/color] %
battery-status-switchable-state = { $state ->
        [on] [color=green]On[/color]
        [off] [color=red]Off[/color]
        *[other] Unknown
}
battery-status-state = State: {$state}

# Charge Status
charge-status-count = Сharges: [color=fuchsia]{$current}/{$max}[/color]
charge-status-recharge = Recharge: [color=yellow]{$seconds}s[/color]

# Tank Pressure Status
tank-pressure-status = Press.: [color=orange]{$pressure} kPa[/color]
tank-status-switchable-state = { $state ->
        [open] [color=red]Open[/color]
        [closed] [color=green]Closed[/color]
        *[other] Unknown
}
tank-status-state = State: {$state}

# Magazine Status
magazine-status-rounds = Rounds: [color=yellow]{$current}/{$max}[/color]

# Guardian Status
guardian-status-used = [color=red]Used[/color]
guardian-status-ready = [color=green]Ready[/color]

# Anomaly Status
anomaly-status-infinite = [color=gold]Infinite charges[/color]
anomaly-status-charges = [color=orange]{$charges} charges[/color]

# Timer Trigger Status
timer-trigger-status-delay = Set Delay: [color=white]{$delay}s[/color]
