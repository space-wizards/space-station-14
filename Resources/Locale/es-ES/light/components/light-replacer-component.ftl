
### Mensajes de Interacción

# Mostrado cuando el jugador intenta reemplazar luz, pero no quedan luces
comp-light-replacer-missing-light = No quedan luces en {THE($light-replacer)}.

# Mostrado cuando el jugador inserta una bombilla de luz dentro del reemplazador de luz
comp-light-replacer-insert-light = Insertas {$bulb} en {THE($light-replacer)}.

# Mostrado cuando el jugador intenta insertar una bombilla de luz rota en el reemplazador de luz
comp-light-replacer-insert-broken-light = No puedes insertar luces rotas!

# Mostrado cuando el jugador refila luz desde la caja de luz
comp-light-replacer-refill-from-storage = Refillas {THE($light-replacer)}.

### Examen 

comp-light-replacer-no-lights = Está vacío.
comp-light-replacer-has-lights = Contiene lo siguiente:
comp-light-replacer-light-listing = {$amount ->
    [one] [color=yellow]{$amount}[/color] [color=gray]{$name}[/color]
    *[other] [color=yellow]{$amount}[/color] [color=gray]{$name}s[/color]
}
