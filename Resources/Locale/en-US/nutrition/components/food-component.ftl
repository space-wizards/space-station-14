### Interaction Messages

food-you-need-to-hold-utensil = Вы должны держать { $utensil }, чтобы съесть это!
food-nom = Ням. { $flavors }
food-swallow = Вы проглатываете { $food }. { $flavors }
food-has-used-storage = Вы не можете съесть { $food } пока внутри что-то есть.
food-system-remove-mask = Сначала вам нужно снять { $entity }.

## System

food-system-you-cannot-eat-any-more = В вас больше не лезет!
food-system-you-cannot-eat-any-more-other = В них больше не лезет!
food-system-try-use-food-is-empty = В { $entity } пусто!
food-system-wrong-utensil = Вы не можете есть { $food } с помощью { $utensil }.
food-system-cant-digest = Вы не можете переварить { $entity }!
food-system-cant-digest-other = Они не могут переварить { $entity }!
food-system-verb-eat = Съесть

## Force feeding

food-system-force-feed = { CAPITALIZE($user) } пытается вам что-то скормить!
food-system-force-feed-success =
    { CAPITALIZE($user) } { CAPITALIZE($user) ->
        [male] накормил
        [female] накормила
        [epicene] накормили
       *[neuter] накормило
    } вам что-то! { $flavors }
food-system-force-feed-success-user = Вы успешно накормили { $target }
