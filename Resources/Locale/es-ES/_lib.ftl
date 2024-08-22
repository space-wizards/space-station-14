### Mensajes especiales usados por cosas internas del localizador.

# Usado internamente por la función PRESION().
zzzz-fmt-pressure = { TOSTRING($divided, "F1") } { $places ->
    [0] kPa
    [1] MPa
    [2] GPa
    [3] TPa
    [4] PBa
    *[5] ???
}

# Utilizado internamente por la función POWERWATTS().
zzzz-fmt-power-watts = { TOSTRING($divided, "F1") } { $places ->
    [0] W
    [1] kW
    [2] MW
    [3] GW
    [4] TW
    *[5] ???
}

# Utilizado internamente por la función POWERJOULES().
# Recordatorio: 1 julio = 1 vatio durante 1 segundo (multiplique vatios por segundos para obtener julios).
# Por lo tanto 1 kilovatio-hora es igual a 3.600.000 julios (3.6MJ)
zzzz-fmt-power-joules = { TOSTRING($divided, "F1") } { $places ->
    [0] J
    [1] kJ
    [2] MJ
    [3] GJ
    [4] TJ
    *[5] ???
}
