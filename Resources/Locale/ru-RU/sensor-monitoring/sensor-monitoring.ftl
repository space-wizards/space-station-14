sensor-monitoring-window-title = Консоль мониторинга датчиков
sensor-monitoring-value-display =
    { $unit ->
        [PressureKpa] { PRESSURE($value) }
        [PowerW] { POWERWATTS($value) }
        [EnergyJ] { POWERJOULES($value) }
        [TemperatureK] { TOSTRING($value, "N3") } K
        [Ratio] { NATURALPERCENT($value) }
        [Moles] { TOSTRING($value, "N3") } моль
       *[Other] { $value }
    }

# ({ TOSTRING(SUB($value, 273.15), "N3") } °C)

