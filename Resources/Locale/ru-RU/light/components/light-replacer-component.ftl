### Interaction Messages

# Shown when player tries to replace light, but there is no lighs left
comp-light-replacer-missing-light = В { $light-replacer } не осталось лампочек.
# Shown when player inserts light bulb inside light replacer
comp-light-replacer-insert-light = Вы вставили { $bulb } в { $light-replacer }.
# Shown when player tries to insert in light replacer brolen light bulb
comp-light-replacer-insert-broken-light = Вы не можете вставлять разбитые лампочки!
# Shown when player refill light from light box
comp-light-replacer-refill-from-storage = Вы пополнили { $light-replacer }.
comp-light-replacer-no-lights = It's empty.
comp-light-replacer-has-lights = It contains the following:
comp-light-replacer-light-listing =
    { $amount ->
        [one] [color=yellow]{ $amount }[/color] [color=gray]{ $name }[/color]
       *[other] [color=yellow]{ $amount }[/color] [color=gray]{ $name }s[/color]
    }
