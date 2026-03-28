# Surgery System

## Draping
surgery-drape-patient = You drape {THE($target)} with the bedsheet.
surgery-not-draped = {CAPITALIZE(THE($target))} needs to be draped with a bedsheet first.
surgery-patient-not-down = The patient must be lying down for surgery.

## Procedure Selection
surgery-no-procedures = No surgical procedures available.
surgery-procedure-started = You begin {$procedure} on {THE($target)}.
surgery-procedure-complete = The procedure is complete. Cauterize to close the wound.

## Step Popups
surgery-step-incision = {CAPITALIZE(THE($user))} makes an incision in {THE($target)}.
surgery-step-retract = {CAPITALIZE(THE($user))} retracts the incision on {THE($target)}.
surgery-step-clamp = {CAPITALIZE(THE($user))} clamps the blood vessels on {THE($target)}.
surgery-step-saw = {CAPITALIZE(THE($user))} saws through tissue on {THE($target)}.
surgery-step-cauterize = {CAPITALIZE(THE($user))} cauterizes the wound on {THE($target)}.
surgery-step-treat-brute = {CAPITALIZE(THE($user))} repairs physical damage on {THE($target)}.
surgery-step-treat-burn = {CAPITALIZE(THE($user))} treats burn damage on {THE($target)}.
surgery-step-remove-organ = {CAPITALIZE(THE($user))} carefully extracts an organ from {THE($target)}.
surgery-step-insert-organ = {CAPITALIZE(THE($user))} carefully inserts an organ into {THE($target)}.

## Alerts
alerts-surgery-draped-name = Surgical Drapes
alerts-surgery-draped-desc = You are draped for surgery. Click to remove the drapes.

## Examine
surgery-examine-active = [color=cyan]{CAPITALIZE(SUBJECT($target))} {CONJUGATE-BE($target)} undergoing {$procedure}.[/color]
surgery-examine-draped = [color=cyan]{CAPITALIZE(SUBJECT($target))} {CONJUGATE-BE($target)} draped with a bedsheet, prepared for surgery.[/color]

## Tool Feedback
surgery-wrong-tool = That's not the right tool for this step.
surgery-step-repeat-done = The treatment has done all it can.

## Organ Operations
surgery-organ-removed = {CAPITALIZE(THE($organ))} has been removed.
surgery-organ-inserted = {CAPITALIZE(THE($organ))} has been inserted.
surgery-organ-already-exists = The patient already has {THE($organ)}.
surgery-no-organ-in-hand = You need to be holding an organ in your other hand.

## Procedure Names
surgery-procedure-tend-wounds-brute = Tend Wounds (Brute)
surgery-procedure-tend-wounds-burn = Tend Wounds (Burn)
surgery-procedure-organ-manipulation = Organ Manipulation
