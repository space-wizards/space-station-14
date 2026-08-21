contraband-examine-text-Minor =
    { $type ->
        *[item] [color={ $color }]Этот предмет считается мелкой контрабандой.[/color]
        [reagent] [color={ $color }]Этот реагент считается мелкой контрабандой.[/color]
    }

contraband-examine-text-Restricted =
    { $type ->
        *[item] [color={ $color }]Этот предмет департаментно ограничен.[/color]
        [reagent] [color={ $color }]Этот реагент департаментно ограничен.[/color]
    }

contraband-examine-text-Restricted-department =
    { $type ->
        *[item] [color={ $color }]Этот предмет ограничен для { $departments }, и может считаться контрабандой.[/color]
        [reagent] [color={ $color }]Этот реагент ограничен для { $departments }, и может считаться контрабандой.[/color]
    }

contraband-examine-text-Major =
    { $type ->
        *[item] [color={ $color }]Этот предмет считается крупной контрабандой.[/color]
        [reagent] [color={ $color }]Этот реагент считается крупной контрабандой.[/color]
    }

contraband-examine-text-GrandTheft =
    { $type ->
        *[item] [color={ $color }]Этот предмет является очень ценной целью для агентов Синдиката![/color]
        [reagent] [color={ $color }]Этот реагент является очень ценной целью для агентов Синдиката![/color]
    }

contraband-examine-text-Highly-Illegal =
    { $type ->
        *[item] [color={ $color }]Этот предмет является крайне незаконной контрабандой![/color]
        [reagent] [color={ $color }]Этот реагент является крайне незаконной контрабандой![/color]
    }

contraband-examine-text-Syndicate =
    { $type ->
        *[item] [color={ $color }]Этот предмет является крайне незаконной контрабандой Синдиката![/color]
        [reagent] [color={ $color }]Этот реагент является крайне незаконной контрабандой Синдиката![/color]
    }

contraband-examine-text-Magical =
    { $type ->
        *[item] [color={ $color }]Этот предмет является крайне незаконной магической контрабандой![/color]
        [reagent] [color={ $color }]Этот реагент является крайне незаконной магической контрабандой![/color]
    }

contraband-examine-text-avoid-carrying-around = [color=red][italic]Вам, вероятно, не стоит носить его с собой без веской причины.[/italic][/color]
contraband-examine-text-in-the-clear = [color=green][italic]Вы должны быть чисты, чтобы носить этот предмет на виду.[/italic][/color]

contraband-examinable-verb-text = Легальность
contraband-examinable-verb-message = Проверить легальность этого предмета.

contraband-department-plural = { $department }
contraband-job-plural = { $job }
