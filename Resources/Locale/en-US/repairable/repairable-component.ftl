### Interaction Messages

# Shown when repairing something
comp-repairable-repair = You finish repairing {PROPER($target) ->
  [true] {""}
  *[false] the{" "}
}{$target} with {PROPER($tool) ->
  [true] {""}
  *[false] the{" "}
}{$tool}
