drink-component-on-use-is-empty = { CAPITALIZE($owner) } пуст!
drink-component-on-examine-is-empty = [color=gray]Пусто[/color]
drink-component-on-examine-is-opened = [color=yellow]Открыто[/color]
drink-component-on-examine-is-sealed = Пломба не повреждена.
drink-component-on-examine-is-unsealed = Пломба разорвана.
drink-component-on-examine-is-full = Полон
drink-component-on-examine-is-mostly-full = Почти полон
drink-component-on-examine-is-half-full = Наполовину полон
drink-component-on-examine-is-half-empty = Наполовину пуст
drink-component-on-examine-is-mostly-empty = Почти пуст
drink-component-on-examine-exact-volume = Полон на { $amount }ед.
drink-component-try-use-drink-not-open = Сначала откройте { $owner }!
drink-component-try-use-drink-is-empty = { CAPITALIZE($entity) } пуст!
drink-component-try-use-drink-cannot-drink = Вы не можете ничего пить!
drink-component-try-use-drink-had-enough = Вы не можете выпить больше!
drink-component-try-use-drink-cannot-drink-other = Они не могут ничего пить!
drink-component-try-use-drink-had-enough-other = Они не могут выпить больше!
drink-component-try-use-drink-success-slurp = Сёрб
drink-component-try-use-drink-success-slurp-taste = Сёрб. { $flavors }
drink-component-force-feed = { CAPITALIZE($user) } пытается вас чем-то напоить!
drink-component-force-feed-success =
    { CAPITALIZE($user) } { GENDER($user) ->
        [male] напоил
        [female] напоила
        [epicene] напоили
       *[neuter] напоило
    } вас чем-то! { $flavors }
drink-component-force-feed-success-user = Вы успешно напоили { $target }
drink-system-verb-drink = Пить
