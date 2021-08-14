### Interaction Messages

# Shown when repairing something
comp-repairable-repair = Вы ремонтируете {PROPER($target) ->
  [true] {""}
  *[false] {" "}
}{$target} с {PROPER($welder) ->
  [true] {""}
  *[false] {" "}
}{$welder}
