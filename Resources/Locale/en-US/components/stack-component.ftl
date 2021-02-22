
### UI

# Shown when a stack is examined in details range
stack-component-examine-detail-count = {$count ->
    [one] There is [color={$markupCountColor}]{$count}[/color] thing
    *[other] There are [color={$markupCountColor}]{$count}[/color] things
} in the stack.

### Interaction Messages

# Shown when attempting to add to a stack that is full
stack-component-already-full = Stack is already full.

# Shown when a stack becomes full
stack-component-becomes-full = Stack is now full.