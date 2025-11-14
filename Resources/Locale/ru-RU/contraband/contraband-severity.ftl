contraband-examine-text-Minor =
    { $type ->
       *[item] [color=yellow]Этот предмет считается мелкой контрабандой.[/color]
        [reagent] [color=yellow]Этот реагент считается мелкой контрабандой.[/color]
    }
contraband-examine-text-Restricted =
    { $type ->
       *[item] [color=yellow]Этот предмет департаментно ограничен.[/color]
        [reagent] [color=yellow]Этот реагент департаментно ограничен.[/color]
    }
contraband-examine-text-Restricted-department =
    { $type ->
       *[item] [color=yellow]Этот предмет ограничен для { $departments }, и может считаться контрабандой.[/color]
        [reagent] [color=yellow]Этот реагент ограничен для { $departments }, и может считаться контрабандой.[/color]
    }
contraband-examine-text-Major =
    { $type ->
       *[item] [color=red]Этот предмет считается крупной контрабандой.[/color]
        [reagent] [color=red]Этот реагент считается крупной контрабандой.[/color]
    }
contraband-examine-text-GrandTheft =
    { $type ->
       *[item] [color=red]Этот предмет является очень ценной целью для агентов Синдиката![/color]
        [reagent] [color=red]Этот реагент является очень ценной целью для агентов Синдиката![/color]
    }
contraband-examine-text-Highly-Illegal =
    { $type ->
       *[item] [color=crimson]Этот предмет является крайне незаконной контрабандой![/color]
        [reagent] [color=crimson]Этот реагент является крайне незаконной контрабандой![/color]
    }
contraband-examine-text-Syndicate =
    { $type ->
       *[item] [color=crimson]Этот предмет является крайне незаконной контрабандой Синдиката![/color]
        [reagent] [color=crimson]Этот реагент является крайне незаконной контрабандой Синдиката![/color]
    }
contraband-examine-text-Magical =
    { $type ->
       *[item] [color=#b337b3]Этот предмет является крайне незаконной магической контрабандой![/color]
        [reagent] [color=#b337b3]Этот реагент является крайне незаконной магической контрабандой![/color]
    }
contraband-examine-text-avoid-carrying-around = [color=red][italic]Вам, вероятно, не стоит носить его с собой без веской причины.[/italic][/color]
contraband-examine-text-in-the-clear = [color=green][italic]Вы должны быть чисты, чтобы носить этот предмет на виду.[/italic][/color]
contraband-examinable-verb-text = Легальность
contraband-examinable-verb-message = Проверить легальность этого предмета.
contraband-department-plural = { $department }
contraband-job-plural = { $job }
