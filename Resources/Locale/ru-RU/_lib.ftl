### Special messages used by internal localizer stuff.

# Used internally by the PRESSURE() function.
zzzz-fmt-pressure =
    { TOSTRING($divided, "G3") } { $places ->
        [0] кПа
        [1] МПа
        [2] ГПа
        [3] ТПа
        [4] ППа
       *[5] ???
    }
# Used internally by the POWERWATTS() function.
zzzz-fmt-power-watts =
    { TOSTRING($divided, "G3") } { $places ->
        [0] Вт
        [1] кВт
        [2] МВт
        [3] ГВт
        [4] ТВт
       *[5] ???
    }
# Used internally by the POWERJOULES() function.
# Reminder: 1 joule = 1 watt for 1 second (multiply watts by seconds to get joules).
# Therefore 1 kilowatt-hour is equal to 3,600,000 joules (3.6MJ)
zzzz-fmt-power-joules =
    { TOSTRING($divided, "G3") } { $places ->
        [0] Дж
        [1] кДж
        [2] МДж
        [3] ГДж
        [4] ТДж
       *[5] ???
    }
# Used internally by the THE() function.
zzzz-the =
    { PROPER($ent) ->
       *[false] the { $ent }
        [true] { $ent }
    }
# Used internally by the SUBJECT() function.
zzzz-subject-pronoun =
    { GENDER($ent) ->
        [male] он
        [female] она
        [epicene] они
       *[neuter] оно
    }
# Used internally by the OBJECT() function.
zzzz-object-pronoun =
    { GENDER($ent) ->
        [male] его
        [female] её
        [epicene] их
       *[neuter] его
    }
# Used internally by the POSS-PRONOUN() function.
zzzz-possessive-pronoun =
    { GENDER($ent) ->
        [male] его
        [female] её
        [epicene] их
       *[neuter] его
    }
# Used internally by the POSS-ADJ() function.
zzzz-possessive-adjective =
    { GENDER($ent) ->
        [male] сам
        [female] сама
        [epicene] сами
       *[neuter] сам
    }
# Used internally by the REFLEXIVE() function.
zzzz-reflexive-pronoun =
    { GENDER($ent) ->
        [male] сам
        [female] сама
        [epicene] сами
       *[neuter] сам
    }
# Used internally by the CONJUGATE-BE() function.
zzzz-conjugate-be =
    { GENDER($ent) ->
        [epicene] are
       *[other] is
    }
# Used internally by the CONJUGATE-HAVE() function.
zzzz-conjugate-have =
    { GENDER($ent) ->
        [epicene] have
       *[other] has
    }
