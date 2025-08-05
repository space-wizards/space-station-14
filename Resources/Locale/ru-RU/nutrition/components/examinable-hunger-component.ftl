examinable-hunger-component-examine-overfed =
    { CAPITALIZE(SUBJECT($entity)) } { CONJUGATE-BASIC($entity, "выглядят", "выглядит") }
    { GENDER($entity) ->
        [male] сытым
        [female] сытой
        [epicene] сытыми
       *[neuter] сытым
    }.
examinable-hunger-component-examine-okay =
    { CAPITALIZE(SUBJECT($entity)) } { CONJUGATE-BASIC($entity, "выглядят", "выглядит") }
    { GENDER($entity) ->
        [male] довольным
        [female] довольной
        [epicene] довольными
       *[neuter] довольным
    }.
examinable-hunger-component-examine-peckish =
    { CAPITALIZE(SUBJECT($entity)) } { CONJUGATE-BASIC($entity, "выглядят", "выглядит") }
    { GENDER($entity) ->
        [male] проголодавшимся
        [female] проголодавшейся
        [epicene] проголодавшимися
       *[neuter] проголодавшимся
    }.
examinable-hunger-component-examine-starving =
    { CAPITALIZE(SUBJECT($entity)) } { CONJUGATE-BASIC($entity, "выглядят", "выглядит") }
    { GENDER($entity) ->
        [male] изголодавшимся
        [female] изголодавшейся
        [epicene] изголодавшимися
       *[neuter] изголодавшимся
    }!
examinable-hunger-component-examine-none = { CAPITALIZE(SUBJECT($entity)) }, похоже, не { CONJUGATE-BASIC($entity, "голодают", "голодает") }.
