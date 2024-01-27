ammonia-smell = Что-то резко попахивает!!
perishable-1 = [color=green]{ CAPITALIZE(SUBJECT($target)) } выглядит ещё свежо.[/color]
perishable-2 = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } выглядит не особо свежо.[/color]
perishable-3 = [color=red]{ CAPITALIZE(SUBJECT($target)) } выглядит совсем не свежо.[/color]
rotting-rotting = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { $gender ->
        [male] гниёт
        [female] гниёт
        [epicene] гниют
       *[neuter] гниёт
    }![/color]
rotting-bloated = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } { $gender ->
        [male] вздулся
        [female] вздулась
        [epicene] вздулись
       *[neuter] вздулось
    }![/color]
rotting-extremely-bloated = [color=red]{ CAPITALIZE(SUBJECT($target)) } сильно { $gender ->
        [male] вздулся
        [female] вздулась
        [epicene] вздулись
       *[neuter] вздулось
    }![/color]
