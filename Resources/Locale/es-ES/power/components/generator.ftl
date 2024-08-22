generator-clogged = {CAPITALIZE(THE($generator))} se apaga abruptamente!

portable-generator-verb-start = Iniciar generador
portable-generator-verb-start-msg-unreliable = Intenta iniciar el generador. Esto puede requerir algunos intentos.
portable-generator-verb-start-msg-reliable = Iniciar el generador.
portable-generator-verb-start-msg-unanchored = ¡El generador debe estar anclado primero!
portable-generator-verb-stop = Detener generador
portable-generator-start-fail = Tiras del cordón, pero no arranca.
portable-generator-start-success = Tiras del cordón y comienza a funcionar.

portable-generator-ui-title = Generador Portátil
portable-generator-ui-status-stopped = Detenido:
portable-generator-ui-status-starting = Iniciando:
portable-generator-ui-status-running = Funcionando:
portable-generator-ui-start = Iniciar
portable-generator-ui-stop = Detener
portable-generator-ui-target-power-label = Potencia objetivo (kW):
portable-generator-ui-efficiency-label = Eficiencia:
portable-generator-ui-fuel-use-label = Uso de combustible:
portable-generator-ui-fuel-left-label = Combustible restante:
portable-generator-ui-clogged = ¡Contaminantes detectados en el tanque de combustible!
portable-generator-ui-eject = Expulsar
portable-generator-ui-eta = (~{ $minutes } min)
portable-generator-ui-unanchored = No anclado
portable-generator-ui-current-output = Salida actual: {$voltage}
portable-generator-ui-network-stats = Red:
portable-generator-ui-network-stats-value = { POWERWATTS($supply) } / { POWERWATTS($load) }
portable-generator-ui-network-stats-not-connected = No conectado

power-switchable-generator-examine = La salida de energía está ajustada a {$voltage}.
power-switchable-generator-switched = ¡Salida cambiada a {$voltage}!

power-switchable-voltage = { $voltage ->
    [HV] [color=orange]HV[/color]
    [MV] [color=yellow]MV[/color]
    *[LV] [color=green]LV[/color]
}
power-switchable-switch-voltage = Cambiar a {$voltage}

fuel-generator-verb-disable-on = ¡Apaga el generador primero!