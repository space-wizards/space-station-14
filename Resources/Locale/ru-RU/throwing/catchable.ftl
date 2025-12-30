catchable-component-success-self = Вы поймали { $item }!
catchable-component-success-others =
    { CAPITALIZE($catcher) } { GENDER($catcher) ->
        [male] поймал
        [female] поймала
        [epicene] поймало
       *[neuter] поймали
    } { $item }!
catchable-component-fail-self = Вы не поймали { $item }!
catchable-component-fail-others =
    { CAPITALIZE($catcher) } не { GENDER($catcher) ->
        [male] поймал
        [female] поймала
        [epicene] поймало
       *[neuter] поймали
    } { $item }!
