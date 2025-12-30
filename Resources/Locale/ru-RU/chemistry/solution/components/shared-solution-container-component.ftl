shared-solution-container-component-on-examine-main-text =
    Содержит [color={ $color }]{ $desc }[/color] { $chemCount ->
        [1] вещество.
       *[other] смесь веществ.
    }
examinable-solution-has-recognizable-chemicals = Вам удаётся распознать { $recognizedString } в этом растворе.
examinable-solution-recognized = [color={ $color }]{ $chemical }[/color]
examinable-solution-on-examine-volume = Ёмкость { $fillLevel ->
    [exact] содержит [color=white]{$current}/{$max} ед[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}
examinable-solution-on-examine-volume-no-max = Ёмкость { $fillLevel ->
    [exact] содержит [color=white]{$current} ед[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}
examinable-solution-on-examine-volume-puddle =
    Лужа { $fillLevel ->
        [exact] содержит [color=white]{ $current } ед[/color].
        [full] огромная и разливается через край!
        [mostlyfull] огромная и разливается через край!
        [halffull] глубокая и растекающаяся.
        [halfempty] средняя.
       *[mostlyempty] собирается в одну.
        [empty] превращается в несколько маленьких луж.
    }
-solution-vague-fill-level =
    { $fillLevel ->
        [full] [color=white]заполнена[/color]
        [mostlyfull] [color=#DFDFDF]почти заполнена[/color]
        [halffull] [color=#C8C8C8]наполовину полная[/color]
        [halfempty] [color=#C8C8C8]наполовину пустая[/color]
        [mostlyempty] [color=#A4A4A4]почти пустая[/color]
       *[empty] [color=gray]пустая[/color]
    }
