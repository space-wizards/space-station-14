### Interaction Messages

# Shown when repairing something
comp-repairable-repair = Вы ремонтируете {PROPER($target) ->
  [true] {""}
 *[false] {" "}
}{$target} с помощью {PROPER($tool) ->
  [true] {""}
 *[false] {" "}
}{$tool}
