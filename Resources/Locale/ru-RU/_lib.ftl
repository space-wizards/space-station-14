### Special messages used by internal localizer stuff.

# Used internally by the PRESSURE() function.
zzzz-fmt-pressure =
    { TOSTRING($divided, "F1") } { $places ->
        [0] кПа
        [1] МПа
        [2] ГПа
        [3] ТПа
        [4] ППа
       *[5] ???
    }
# Used internally by the POWERWATTS() function.
zzzz-fmt-power-watts =
    { TOSTRING($divided, "F1") } { $places ->
        [0] Вт
        [1] кВт
        [2] МВт
        [3] ГВт
        [4] ТВт
       *[5] ???
    }
# Used internally by the POWERJOULES() function.
# Reminder: 1 joule = 1 watt for 1 second (multiply watts by seconds to get joules).
# Therefore 1 kilowatt-hour is equal to 3,600,000 joules (3.6MJ)
zzzz-fmt-power-joules =
    { TOSTRING($divided, "F1") } { $places ->
        [0] Дж
        [1] кДж
        [2] МДж
        [3] ГДж
        [4] ТДж
       *[5] ???
    }
