### UI

# Mostrado cuando se examina una pila en detalle
comp-stack-examine-detail-count = {$count ->
    [one] Hay [color={$markupCountColor}]{$count}[/color] cosa
    *[other] Hay [color={$markupCountColor}]{$count}[/color] cosas
} en la pila.

# Control del estado de la pila
comp-stack-status = Contar: [color=white]{$count}[/color]

### Mensajes de Interacción

# Mostrado cuando se intenta agregar a una pila que está llena
comp-stack-already-full = La pila ya está llena.

# Mostrado cuando una pila se llena
comp-stack-becomes-full = La pila ahora está llena.

# Texto relacionado con la división de una pila
comp-stack-split = Has dividido la pila.
comp-stack-split-halve = Dividir a la mitad
comp-stack-split-too-small = La pila es demasiado pequeña para dividirla.
