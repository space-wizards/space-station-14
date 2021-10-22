### UI

# Shown when a stack is examined in details range
comp-stack-examine-detail-count = {$count ->
    [one] There is [color={$markupCountColor}]{$count}[/color] thing
    *[other] There are [color={$markupCountColor}]{$count}[/color] things
} in the stack.

# Stack status control
comp-stack-status = Count: [color=white]{$count}[/color]

### Interaction Messages

# Shown when attempting to add to a stack that is full
comp-stack-already-full = Stack is already full.

# Shown when a stack becomes full
comp-stack-becomes-full = Stack is now full.

# Text related to splitting a stack
comp-stack-split = You split the stack.
comp-stack-split-halve = Halve
comp-stack-split-too-small = Stack is too small to split.
