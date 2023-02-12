guidebook-reagent-effect-description =
    {$chance ->
        [1] { $effect }
        *[other] Has a { TOSTRING($chance, "p2") } chance to { $effect }
    }{ $conditionCount ->
        [0] .
        *[other] {" "}when { $conditions }.
    }

