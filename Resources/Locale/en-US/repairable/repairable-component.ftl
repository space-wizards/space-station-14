### Interaction Messages

# Shown when repairing something
comp-repairable-repair = You repair {PROPER($target) ->
  [true] {""}
  *[false] the{" "}
}{$target} with {PROPER($tool) ->
  [true] {""}
  *[false] the{" "}
}{$tool}

comp-repairable-replacement-repair = You repair {PROPER($target) ->
  [true] {""}
  *[false] the{" "}
}{$target} with {$amountUsed} {$material}
