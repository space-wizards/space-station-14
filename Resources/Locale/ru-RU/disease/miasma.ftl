ammonia-smell = Что-то резко попахивает!!
perishable-1 = [color=green]{ CAPITALIZE(OBJECT($target)) } тело выглядит ещё свежим.[/color]
perishable-2 = [color=orangered]{ CAPITALIZE(OBJECT($target)) } тело выглядит не особо свежим.[/color]
perishable-3 = [color=red]{ CAPITALIZE(OBJECT($target)) } тело выглядит совсем не свежим.[/color]
perishable-1-nonmob = [color=green]{ CAPITALIZE(SUBJECT($target)) } выглядит ещё свежо.[/color]
perishable-2-nonmob = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } выглядит не особо свежо.[/color]
perishable-3-nonmob = [color=red]{ CAPITALIZE(SUBJECT($target)) } выглядит не особо свежо.[/color]
rotting-rotting = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { GENDER($target) ->
        [male] гниёт
        [female] гниёт
        [epicene] гниют
       *[neuter] гниёт
    }![/color]
rotting-bloated = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } { GENDER($target) ->
        [male] вздулся
        [female] вздулась
        [epicene] вздулись
       *[neuter] вздулось
    }![/color]
rotting-extremely-bloated = [color=red]{ CAPITALIZE(SUBJECT($target)) } сильно { GENDER($target) ->
        [male] вздулся
        [female] вздулась
        [epicene] вздулись
       *[neuter] вздулось
    }![/color]
rotting-rotting-nonmob = [color=orange]{ CAPITALIZE(SUBJECT($target)) } гниёт![/color]
rotting-bloated-nonmob = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } вздулось![/color]
rotting-extremely-bloated-nonmob = [color=red]{ CAPITALIZE(SUBJECT($target)) } сильно вздулось![/color]
