### UI status control
comp-solution-transfer-draw-text = fill
comp-solution-transfer-inject-text = spill
comp-solution-transfer-invalid-toggle-mode = Nothing can do
comp-solution-transfer-status-volume-label = Volume: {$currentVolume}/{$totalVolume}

### Solution transfer component
comp-solution-transfer-fill-normal = You fill {THE($target)} with {$amount}u from {THE($owner)}.
comp-solution-transfer-fill-fully = You fill {THE($target)} to the brim with {$amount}u from {THE($owner)}.
comp-solution-transfer-transfer-solution = You transfer {$amount}u to {THE($target)}.

## Displayed when trying to transfer to a solution, but either the giver is empty or the taker is full
comp-solution-transfer-is-empty = {THE($target)} is empty!
comp-solution-transfer-is-full = {THE($target)} is full!

## Displayed in change transfer mode in verb's name
comp-solution-transfer-verb-draw = Pour into {THE($target)}
comp-solution-transfer-verb-inject = Pour from {THE($target)}

## Displayed in change transfer amount verb's name
comp-solution-transfer-verb-custom-amount = Custom
comp-solution-transfer-verb-amount = {$amount}u

## Displayed after you successfully change a solution's amount using the BUI
comp-solution-transfer-set-amount = Transfer amount set to {$amount}u.

## Toggle solution transfer mode, popups
comp-solution-transfer-set-toggle-mode-draw = Now you're pouring in {THE($fromIn)}
comp-solution-transfer-set-toggle-mode-inject = Now you're pouring out {THE($fromIn)}
comp-solution-transfer-cant-change-mode = You can't toggle the pour mode

## Displayed when you can't pour because the targe container hasn't refillable component
comp-solution-transfer-no-target-refillable-component = You can't spill into {THE($target)}
