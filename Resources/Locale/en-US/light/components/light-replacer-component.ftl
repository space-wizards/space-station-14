
### Interaction Messages

# Shown when player tries to replace light, but there is no lights left
comp-light-replacer-missing-light = No  {$light-name}s left in {THE($light-replacer)}.

# Shown when player inserts light bulb inside light replacer
comp-light-replacer-insert-light = You insert {$bulb} into {THE($light-replacer)}.

# Shown when player tries to insert in light replacer broken light bulb
comp-light-replacer-insert-broken-light = You can't insert broken lights!

# Shown when player refill light from light box
comp-light-replacer-refill-from-storage = You refill {THE($light-replacer)}.

# Shown when a player attempts to replace a light with the same color & type as the active light.
comp-light-replacer-same-light = This fixture already holds a {$light}!

# Radial Menu messages
comp-light-replacer-eject-specified-lights = Eject all {$light}s.
comp-light-replacer-select-lights = Select {$light}s.
comp-light-replacer-open-empty = {CAPITALIZE(THE($light-replacer))} is completely empty!

# Label
comp-light-replacer-label = Tube: {$tube}
                            Bulb: {$bulb}

### Examine

comp-light-replacer-no-lights = It's empty.
comp-light-replacer-has-lights = It contains the following:
comp-light-replacer-light-listing = {$amount ->
    [one] [color=yellow]{$amount}[/color] [color=gray]{$name}[/color]
    *[other] [color=yellow]{$amount}[/color] [color=gray]{$name}s[/color]
}
