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
        [male] его
        [female] её
        [epicene] их
       *[neuter] его
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
# Used internally by the CONJUGATE-BASIC() function.
zzzz-conjugate-basic =
    { GENDER($ent) ->
        [epicene] { $first }
       *[other] { $second }
    }
