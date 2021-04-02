### Interaction Messages

# Shown when repairing something
comp-repairable-repair = You repair {PROPER($target) ->
  [true] {""}
  *[false] the{" "}
}{$target} with {PROPER($welder) ->
  [true] {""}
  *[false] the{" "}
}{$welder}
