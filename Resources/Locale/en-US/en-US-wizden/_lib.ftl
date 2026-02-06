### Special messages used by internal localizer stuff.

# Used internally by the PRESSURE() function.
zzzz-fmt-pressure = { TOSTRING($divided, "F1") } { $places ->
    [0] kPa
    [1] MPa
    [2] GPa
    [3] TPa
    [4] PBa
    *[5] ???
}

# Used internally by the POWERWATTS() function.
zzzz-fmt-power-watts = { TOSTRING($divided, "F1") } { $places ->
    [0] W
    [1] kW
    [2] MW
    [3] GW
    [4] TW
    *[5] ???
}

# Used internally by the POWERJOULES() function.
# Reminder: 1 joule = 1 watt for 1 second (multiply watts by seconds to get joules).
# Therefore 1 kilowatt-hour is equal to 3,600,000 joules (3.6MJ)
zzzz-fmt-power-joules = { TOSTRING($divided, "F1") } { $places ->
    [0] J
    [1] kJ
    [2] MJ
    [3] GJ
    [4] TJ
    *[5] ???
}

# Used internally by the ENERGYWATTHOURS() function.
zzzz-fmt-energy-watt-hours = { TOSTRING($divided, "F1") } { $places ->
    [0] Wh
    [1] kWh
    [2] MWh
    [3] GWh
    [4] TWh
    *[5] ???
}

# Used internally by the PLAYTIME() function.
zzzz-fmt-playtime = {$hours}H {$minutes}M
