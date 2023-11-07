guidebook-reagent-effect-description =
    { $chance ->
        [1] { $effect }
       *[other] Имеет { NATURALPERCENT($chance, 2) } шанс { $effect }
    }{ $conditionCount ->
        [0] .
       *[other] { " " }, пока { $conditions }.
    }
guidebook-reagent-name = [bold][color={ $color }]{ CAPITALIZE($name) }[/color][/bold]
guidebook-reagent-recipes-header = Рецепт
guidebook-reagent-recipes-reagent-display = [bold]{ $reagent }[/bold] \[{ $ratio }\]
guidebook-reagent-recipes-mix = Смешайте
guidebook-reagent-recipes-mix-and-heat = Смешайте при температуре выше { $temperature }К
guidebook-reagent-effects-header = Эффекты
guidebook-reagent-effects-metabolism-group-rate = [bold]{ $group }[/bold] [color=gray]({ $rate } единиц в секунду)[/color]
guidebook-reagent-physical-description = На вид вещество { $description }.
