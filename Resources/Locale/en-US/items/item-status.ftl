# Battery Status
battery-status-charge = Charge: [color=#5E7C16]{$percent}[/color] %
battery-status-switchable-state = { $state ->
        [on] [color=green]On[/color]
        [off] [color=red]Off[/color]
        *[other] Unknown
}
battery-status-state = State: {$state}

# Charge Status
charge-status-count = {$name}: [color=white]{$current}/{$max}[/color]
charge-status-recharge = Recharge: [color=yellow]{$seconds}s[/color]
charge-status-name-charges = Сharges
charge-status-name-uses = Uses

# Tank Pressure Status
tank-pressure-status = Press.: [color=orange]{$pressure} kPa[/color]
tank-status-open = [color=red]Open[/color]
tank-status-closed = [color=green]Closed[/color]

# Magazine Status
magazine-status-rounds = Rounds: [color=yellow]{$current}/{$max}[/color]
