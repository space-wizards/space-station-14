lathe-menu-title = Lathe Menu
lathe-menu-queue = Queue
lathe-menu-server-list = Server list
lathe-menu-sync = Sync
lathe-menu-search-designs = Search designs
lathe-menu-category-all = All
lathe-menu-search-filter = Filter:
lathe-menu-amount = Amount:
lathe-menu-reagent-slot-examine = It has a slot for a beaker on the side.
lathe-reagent-dispense-no-container = Liquid pours out of {THE($name)} onto the floor!
lathe-menu-result-reagent-display = {$reagent} ({$amount}u)
lathe-menu-material-display = {$material} ({$amount})
lathe-menu-tooltip-display = {$amount} of {$material}
lathe-menu-description-display = [italic]{$description}[/italic]
lathe-menu-material-amount = { $amount ->
    [1] {NATURALFIXED($amount, 2)} {$unit}
    *[other] {NATURALFIXED($amount, 2)} {MAKEPLURAL($unit)}
}
lathe-menu-material-amount-missing = { $amount ->
    [1] {NATURALFIXED($amount, 2)} {$unit} of {$material} ([color=red]{NATURALFIXED($missingAmount, 2)} {$unit} missing[/color])
    *[other] {NATURALFIXED($amount, 2)} {MAKEPLURAL($unit)} of {$material} ([color=red]{NATURALFIXED($missingAmount, 2)} {MAKEPLURAL($unit)} missing[/color])
}
lathe-menu-no-materials-message = No materials loaded.
lathe-menu-fabricating-message = Fabricating...
lathe-menu-materials-title = Materials
lathe-menu-queue-title = Build Queue
