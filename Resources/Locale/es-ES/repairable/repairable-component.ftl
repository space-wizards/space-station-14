### Mensajes de Interacción

# Mostrado cuando se repara algo
comp-repairable-repair = Reparaste {PROPER($target) ->
  [true] {""}
  *[false] el{" "}
}{$target} con {PROPER($tool) ->
  [true] {""}
  *[false] el{" "}
}{$tool}
