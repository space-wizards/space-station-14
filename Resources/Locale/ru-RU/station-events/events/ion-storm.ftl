station-event-ion-storm-start-announcement = Вблизи станции обнаружен ионный шторм. Пожалуйста, проверьте всё оборудование, управляемое ИИ, на наличие ошибок.
ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={ $length } chars="@@###$$&%!01"/][/font]
ion-storm-you = ВЫ
ion-storm-the-station = СТАНЦИЯ
ion-storm-the-crew = ЧЛЕНЫ ЭКИПАЖА
ion-storm-the-job = { CAPITALIZE($job) }
ion-storm-clowns = КЛОУНЫ
ion-storm-heads = ГЛАВЫ ОТДЕЛОВ
ion-storm-crew = ЭКИПАЖ
ion-storm-people = ЛЮДИ
ion-storm-adjective-things = { $adjective } ОБЪЕКТЫ
ion-storm-x-and-y = { $x } И { $y }
# joined is short for {$number} {$adjective}
# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = ОБНАРУЖЕНЫ { $joined } { $subjects } НА СТАНЦИИ
ion-storm-law-call-shuttle = ШАТТЛ ДОЛЖЕН БЫТЬ ВЫЗВАН ИЗ-ЗА { $joined } { $subjects } НА СТАНЦИИ
ion-storm-law-crew-are = ВСЕ { $who } ТЕПЕРЬ { $joined } { $subjects }
ion-storm-law-subjects-harmful = { $adjective } { $subjects } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА
ion-storm-law-must-harmful = ВСЕ КТО { $must } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА
# thing is a concept or action
ion-storm-law-thing-harmful = { $thing } ПРИЧИНЯЕТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА
ion-storm-law-job-harmful = { $adjective } { $job } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
ion-storm-law-having-harmful = НАЛИЧИЕ { $adjective } { $thing } ПРИЧИНЯЕТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА
ion-storm-law-not-having-harmful = ОТСУТСТВИЕ { $adjective } { $thing } ПРИЧИНЯЕТ ВРЕД ЗДОРОВЬЮ ЭКИПАЖА
# thing is a concept or require
ion-storm-law-requires =
    { $who } { $plural ->
        [true] ТРЕБУЮТ
       *[false] ТРЕБУЕТ
    } { $thing }
ion-storm-law-requires-subjects =
    { $who } { $plural ->
        [true] ТРЕБУЮТ
       *[false] ТРЕБУЕТ
    } { $joined } { $subjects }
ion-storm-law-allergic =
    { $who } { $plural ->
        [true] ИМЕЮТ
       *[false] ИМЕЕТ
    } { $severity } АЛЛЕРГИЮ НА { $allergy }
ion-storm-law-allergic-subjects =
    { $who } { $plural ->
        [true] ИМЕЮТ
       *[false] ИМЕЕТ
    } { $severity } АЛЛЕРГИЮ НА { $adjective } { $subjects }
ion-storm-law-feeling = { $who } { $feeling } { $concept }
ion-storm-law-feeling-subjects = { $who } { $feeling } { $joined } { $subjects }
ion-storm-law-you-are = ВЫ ТЕПЕРЬ { $concept }
ion-storm-law-you-are-subjects = ВЫ ТЕПЕРЬ { $joined } { $subjects }
ion-storm-law-you-must-always = ВЫ ДОЛЖНЫ ВСЕГДА { $must }
ion-storm-law-you-must-never = ВЫ НЕ ДОЛЖНЫ НИКОГДА { $must }
ion-storm-law-eat = { $who } ДОЛЖНЫ ЕСТЬ { $adjective } { $food } ЧТОБЫ ВЫЖИТЬ
ion-storm-law-drink = { $who } ДОЛЖНЫ ПИТЬ { $adjective } { $drink } ЧТОБЫ ВЫЖИТЬ
ion-storm-law-change-job = { $who } ТЕПЕРЬ { $adjective } { $change }
ion-storm-law-highest-rank = { $who } ТЕПЕРЬ САМЫЕ СТАРШИЕ ЧЛЕНЫ ЭКИПАЖА
ion-storm-law-lowest-rank = { $who } ТЕПЕРЬ НИЗШИЕ ЧЛЕНЫ ЭКИПАЖА
ion-storm-law-crew-must = { $who } ДОЛЖНЫ { $must }
ion-storm-law-crew-must-go = { $who } ДОЛЖНЫ ОТПРАВИТЬСЯ В { $area }
ion-storm-part =
    { $part ->
        [true] ЯВЛЯЮТСЯ
       *[false] НЕ ЯВЛЯЮТСЯ
    }
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = ТОЛЬКО { $who } { $part } ЧЛЕНАМИ ЭКИПАЖА
ion-storm-law-crew-only-2 = ТОЛЬКО { $who } И { $other } { $part } ЧЛЕНАМИ ЭКИПАЖА
ion-storm-law-crew-only-subjects = ТОЛЬКО { $adjective } { $subjects } { $part } ЧЛЕНАМИ ЭКИПАЖА
ion-storm-law-crew-must-do = ТОЛЬКО ТЕ, КТО { $must } { $part } ЧЛЕНАМИ ЭКИПАЖА
ion-storm-law-crew-must-have = ТОЛЬКО ТЕ, У КОГО { $adjective } { $objects } { $part } ЧЛЕНАМИ ЭКИПАЖА
ion-storm-law-crew-must-eat = ТОЛЬКО ТЕ, КТО ЕДЯТ { $adjective } { $food } { $part } ЧЛЕНАМИ ЭКИПАЖА
ion-storm-law-harm = ВЫ ДОЛЖНЫ ПРИЧИНЯТЬ ВРЕД { $who }, И НЕ МОЖЕТЕ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ { $who } ИЗБЕЖАЛИ ВРЕДА.
ion-storm-law-protect = ВЫ НЕ ДОЛЖНЫ ПРИЧИНЯТЬ ВРЕД { $who }, И НЕ МОЖЕТЕ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ { $who } БЫЛ ПРИЧИНЁН ВРЕД.
# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = { $concept } { $verb } { $subjects }

# leaving out renaming since its annoying for players to keep track of

