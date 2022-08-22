### UI

# Shown when a stack is examined in details range
comp-stack-examine-detail-count = {$count ->
    [one] Há [color={$markupCountColor}]{$count}[/color] coisa
    *[other] Há [color={$markupCountColor}]{$count}[/color] coisas
} na pilha.

# Stack status control
comp-stack-status = Quantidade: [color=white]{$count}[/color]

### Interaction Messages

# Shown when attempting to add to a stack that is full
comp-stack-already-full = A Pilha já está cheia.

# Shown when a stack becomes full
comp-stack-becomes-full = A Pilha agora está cheia.

# Text related to splitting a stack
comp-stack-split = Você separa a pilha.
comp-stack-split-halve = Dividir no Meio
comp-stack-split-too-small = A Pilha é muito pequena para separar.
