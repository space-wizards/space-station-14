### Interaction Messages

# Shown when player tries to replace light, but there is no lights left
comp-light-replacer-missing-light = No {$light-name}s left in {THE($light-replacer)}.

# Shown when player tries to insert in light replacer broken light bulb
comp-light-replacer-insert-broken-light = You can't insert broken lights!

# Shown when a player attempts to replace a light with the same color & type as the active light.
comp-light-replacer-same-light = This fixture already holds a {$light}!

# Radial Menu messages
comp-light-replacer-switch-light = You switched to {$light}s.
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

### Status Control

# Bulbs
comp-light-bulb-incandescent = incandescent
comp-light-bulb-dim = dim
comp-light-bulb-warm = warm
comp-light-bulb-service = service

# Tubes
comp-light-bulb-fluorescent = fluorescent
comp-light-bulb-exterior = exterior
comp-light-bulb-sodium = sodium

# Both
comp-light-bulb-old = old
comp-light-bulb-led = led
comp-light-bulb-cyan = cyan
comp-light-bulb-blue = blue
comp-light-bulb-yellow = yellow
comp-light-bulb-pink = pink
comp-light-bulb-orange = orange
comp-light-bulb-black = black
comp-light-bulb-red = red
comp-light-bulb-green = green
