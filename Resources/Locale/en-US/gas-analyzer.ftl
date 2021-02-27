### UI

# Item status of the gas analyzer. {$status} indicates how dangerous the surrounding area is
comp-gas-analyzer-pressure-status = Pressure: [color={$color}]{$status}[/color]

# Gas analyzer menu current zone status information
gas-analyzer-error = Error: {$error}
gas-analyzer-pressure = Pressure: { PRESSURE($pressure) }
gas-analyzer-temperature = Temperature: {$kelvin}K { TOSTRING($celsius, "(0.## C°)") }
# Gas information
gas-analyzer-gas-amount = { TOSTRING($amount, "0.##") } mol
gas-analyzer-gas-info = {$name}: { TOSTRING($amount, "0.##") } mol {$percentage} %
