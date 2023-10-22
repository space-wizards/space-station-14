### UI
comp-solution-transfer-draw-text = pour from
comp-solution-transfer-inject-text = spill in
comp-solution-transfer-invalid-toggle-mode = Invalid action
comp-solution-transfer = Volume: [color=white]{$currentVolume}/{$totalVolume}[/color]

### Solution transfer component
comp-solution-transfer-fill-normal = You fill {THE($target)} with {$amount}u from {THE($owner)}.
comp-solution-transfer-fill-fully = You fill {THE($target)} to the brim with {$amount}u from {THE($owner)}.
comp-solution-transfer-transfer-solution = You transfer {$amount}u to {THE($target)}.

## Displayed when trying to transfer to a solution, but either the giver is empty or the taker is full
comp-solution-transfer-is-empty = {THE($target)} is empty!
comp-solution-transfer-is-full = {THE($target)} is full!

## Displayed in change transfer amount verb's name
comp-solution-transfer-verb-custom-amount = Custom
comp-solution-transfer-verb-amount = {$amount}u

## Displayed after you successfully change a solution's amount using the BUI
comp-solution-transfer-set-amount = Transfer amount set to {$amount}u.

##Toggle solution transfer mode
comp-solution-transfer-set-toggle-mode = Now you {$mode}
comp-solution-transfer-cant-change-mode = You can't toggle the pour mode

##Con't pour
comp-solution-transfer-no-solution-target = You can't {$mode} {THE($target)}
