anomaly-component-contact-damage = ¡La anomalía quema tu piel!

anomaly-vessel-component-anomaly-assigned = Anomalía asignada al recipiente.
anomaly-vessel-component-not-assigned = Este recipiente no está asignado a ninguna anomalía. Intenta usar un escáner activo en ella.
anomaly-vessel-component-assigned = Este recipiente está actualmente asignado a una anomalía.

anomaly-particles-delta = Partículas Delta
anomaly-particles-epsilon = Partículas Epsilon
anomaly-particles-zeta = Partículas Zeta
anomaly-particles-omega = Partículas Omega
anomaly-particles-sigma = Partículas Sigma

anomaly-scanner-component-scan-complete = ¡Escaneo completado!

anomaly-scanner-ui-title = Escáner de Anomalías
anomaly-scanner-no-anomaly = No se ha escaneado ninguna anomalía actualmente.
anomaly-scanner-severity-percentage = Severidad actual: [color=gray]{$percent}[/color]
anomaly-scanner-severity-percentage-unknown = Severidad actual: [color=red]ERROR[/color]
anomaly-scanner-stability-low = Estado actual de la anomalía: [color=gold]En descomposición[/color]
anomaly-scanner-stability-medium = Estado actual de la anomalía: [color=forestgreen]Estable[/color]
anomaly-scanner-stability-high = Estado actual de la anomalía: [color=crimson]Creciendo[/color]
anomaly-scanner-stability-unknown = Estado actual de la anomalía: [color=red]ERROR[/color]
anomaly-scanner-point-output = Salida de puntos: [color=gray]{$point}[/color]
anomaly-scanner-point-output-unknown = Salida de puntos: [color=red]ERROR[/color]
anomaly-scanner-particle-readout = Análisis de Reacción de Partículas:
anomaly-scanner-particle-danger = - [color=crimson]Peligro:[/color] {$type}
anomaly-scanner-particle-unstable = - [color=plum]Inestabilizar:[/color] {$type}
anomaly-scanner-particle-containment = - [color=goldenrod]Contención:[/color] {$type}
anomaly-scanner-particle-transformation = - [color=#6b75fa]Transformación:[/color] {$type}
anomaly-scanner-particle-danger-unknown = - [color=crimson]Peligro:[/color] [color=red]ERROR[/color]
anomaly-scanner-particle-unstable-unknown = - [color=plum]Inestable:[/color] [color=red]ERROR[/color]
anomaly-scanner-particle-containment-unknown = - [color=goldenrod]Contención:[/color] [color=red]ERROR[/color]
anomaly-scanner-particle-transformation-unknown = - [color=#6b75fa]Transformación:[/color] [color=red]ERROR[/color]
anomaly-scanner-pulse-timer = Tiempo hasta el próximo pulso: [color=gray]{$time}[/color]

anomaly-gorilla-core-slot-name = Núcleo de Anomalía
anomaly-gorilla-charge-none = No tiene ningún [bold]núcleo de anomalía[/bold] dentro de él.
anomaly-gorilla-charge-limit = Tiene [color={$count ->
    [3]green
    [2]yellow
    [1]orange
    [0]red
    *[other]purple
}]{$count} {$count ->
    [one]carga
    *[other]cargas
}[/color] restantes.
anomaly-gorilla-charge-infinite = Tiene [color=gold]cargas infinitas[/color]. [italic]Por ahora...[/italic]

anomaly-sync-connected = Anomalía adjunta con éxito
anomaly-sync-disconnected = ¡Se ha perdido la conexión con la anomalía!
anomaly-sync-no-anomaly = No hay anomalías en el rango.
anomaly-sync-examine-connected = Está [color=darkgreen]adjunto[/color] a una anomalía.
anomaly-sync-examine-not-connected = [color=darkred]No está adjunto[/color] a una anomalía.
anomaly-sync-connect-verb-text = Adjuntar anomalía
anomaly-sync-connect-verb-message = Adjunta una anomalía cercana a {THE($machine)}.

anomaly-generator-ui-title = Generador de Anomalías
anomaly-generator-fuel-display = Combustible:
anomaly-generator-cooldown = Tiempo de reutilización: [color=gray]{$time}[/color]
anomaly-generator-no-cooldown = Tiempo de reutilización: [color=gray]Completo[/color]
anomaly-generator-yes-fire = Estado: [color=forestgreen]Listo[/color]
anomaly-generator-no-fire = Estado: [color=crimson]No listo[/color]
anomaly-generator-generate = Generar Anomalía
anomaly-generator-charges = {$charges ->
    [one] {$charges} carga
    *[other] {$charges} cargas
}
anomaly-generator-announcement = ¡Se ha generado una anomalía!

anomaly-command-pulse = Emite pulsos a una anomalía objetivo
anomaly-command-supercritical = Hace que una anomalía objetivo se vuelva supercrítica

# Texto de sabor en el pie
anomaly-generator-flavor-left = La anomalía puede aparecer dentro del operador.
anomaly-generator-flavor-right = v1.1

anomaly-behavior-unknown = [color=red]ERROR. No se puede leer.[/color]

anomaly-behavior-title = Análisis de desviación de comportamiento:
anomaly-behavior-point = [color=gold]La anomalía produce {$mod}% de los puntos[/color]

anomaly-behavior-safe = [color=forestgreen]La anomalía es extremadamente estable. Pulsaciones poco frecuentes.[/color]
anomaly-behavior-slow = [color=forestgreen]La frecuencia de pulsaciones es mucho menor.[/color]
anomaly-behavior-light = [color=forestgreen]La potencia de pulsación está significativamente reducida.[/color]
anomaly-behavior-balanced = No se detectaron desviaciones de comportamiento.
anomaly-behavior-delayed-force = La frecuencia de pulsaciones está muy reducida, pero su potencia está aumentada.
anomaly-behavior-rapid = La frecuencia de pulsaciones es mucho mayor, pero su fuerza está atenuada.
anomaly-behavior-reflect = Se detectó un revestimiento protector.
anomaly-behavior-nonsensivity = Se detectó una reacción débil a las partículas.
anomaly-behavior-sensivity = Se detectó una reacción amplificada a las partículas.
anomaly-behavior-invisibility = Se ha detectado distorsión de las ondas de luz.
anomaly-behavior-secret = Interferencia detectada. Algunos datos no se pueden leer.
anomaly-behavior-inconstancy = [color=crimson]Se ha detectado impermanencia. Los tipos de partículas pueden cambiar con el tiempo.[/color]
anomaly-behavior-fast = [color=crimson]La frecuencia de pulsaciones está fuertemente aumentada.[/color]
anomaly-behavior-strenght = [color=crimson]La potencia de pulsación está significativamente aumentada.[/color]
anomaly-behavior-moving = [color=crimson]Se detectó inestabilidad en las coordenadas.[/color]