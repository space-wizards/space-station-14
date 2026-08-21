guidebook-reagent-effect-description =
    { $chance ->
        [1] { $effect }
        *[other] Имеет { NATURALPERCENT($chance, 2) } шанс { $effect }
    }{$quantity ->
        [0] { "" }
        *[other] , если имеется как минимум { $quantity } ед. { $reagent }
    }{ $conditionCount ->
        [0] .
        *[other] , пока { $conditions }.
    }

guidebook-reagent-name = [bold][color={ $color }]{ CAPITALIZE($name) }[/color][/bold]
guidebook-reagent-recipes-header = Рецепт
guidebook-reagent-recipes-reagent-display = [bold]{ $reagent }[/bold] \[{ $ratio }\]
guidebook-reagent-sources-header = Источники
guidebook-reagent-sources-ent-wrapper = [bold]{ $name }[/bold] \[1\]
guidebook-reagent-sources-gas-wrapper = [bold]{ $name } (газ)[/bold] \[1\]
guidebook-reagent-effects-header = Эффекты
guidebook-reagent-effects-metabolism-stage-rate = [bold]{ $stage }[/bold] [color=gray]({ $rate } ед. в секунду)[/color]
guidebook-reagent-effects-metabolite-item = { $reagent } с коэффициентом { NATURALPERCENT($rate, 2) }
guidebook-reagent-effects-metabolites = Метаболизируется в { $items }.
guidebook-reagent-plant-metabolisms-header = Метаболизм растений
guidebook-reagent-plant-metabolisms-rate = [bold]Метаболизм растений[/bold] [color=gray](1 единица каждые 3 секунды базово)[/color]
guidebook-reagent-physical-description = [italic]На вид вещество { $description }.[/italic].
guidebook-reagent-recipes-mix-info = { $minTemp ->
    [0] { $hasMax ->
            [true] { CAPITALIZE($verb) } ниже { $maxTemp }K
            *[false] { CAPITALIZE($verb) }
        }
    *[other] { CAPITALIZE($verb) } { $hasMax ->
            [true] между { $minTemp }K и { $maxTemp }K
            *[false] выше { $minTemp }K
        }
}
