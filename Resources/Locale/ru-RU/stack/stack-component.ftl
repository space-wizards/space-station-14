### UI

# Shown when a stack is examined in details range
comp-stack-examine-detail-count = {$count ->
    [one] Есть [color={$markupCountColor}]{$count}[/color] вещь
    *[other] Есть [color={$markupCountColor}]{$count}[/color] вещей
} в стеке.

# Stack status control
comp-stack-status = Количество: [color=white]{$count}[/color]

### Interaction Messages

# Shown when attempting to add to a stack that is full
comp-stack-already-full = Стек уже заполнен.

# Shown when a stack becomes full
comp-stack-becomes-full = Стек теперь заполнен.
