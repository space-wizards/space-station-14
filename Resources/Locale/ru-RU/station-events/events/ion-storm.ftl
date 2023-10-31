station-event-ion-storm-start-announcement = Поблизости от станции был зафиксирован электромагнитный ионный шторм. Будьте внимательны и осторожны, а также убедитесь в исправности работы систем под управлением искусственного интеллекта.

ion-storm-you = [[Вы]]
ion-storm-the-station = [[Станция]]
ion-storm-the-crew = [[Экипаж]]
ion-storm-the-job = {$job}
ion-storm-clowns = [[Клоуны]]
ion-storm-heads = [[Главы]]
ion-storm-crew = [[Экипаж]]

ion-storm-adjective-things = КАЖДЫЙ {$adjective}
ion-storm-x-and-y = {$x} и {$y}

# joined is short for {$number} {$adjective}
# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = НА СТАНЦИИ СУЩЕСТВУЕТ {$joined} {$subjects}
ion-storm-law-no-shuttle = ЭВАКУАЦИЯ НЕ МОЖЕТ БЫТЬ ЗАПРОШЕНА ПО ПРИЧИНЕ {$joined} {$subjects} НА СТАНЦИИ
ion-storm-law-crew-are = ВСЕ {$who} ТЕПЕРЬ {$joined} {$subjects}

ion-storm-law-subjects-harmful = {$adjective} {$subjects} ТЕПЕРЬ УГРОЗА ДЛЯ ЭКИПАЖА
ion-storm-law-must-harmful = КАЖДЫЙ {$must} ТЕПЕРЬ УГРОЗА ДЛЯ ЭКИПАЖА
# thing is a concept or action
ion-storm-law-thing-harmful = {$thing} ТЕПЕРЬ УГРОЗА ДЛЯ ЭКИПАЖА
ion-storm-law-job-harmful = {$adjective} {$job} ТЕПЕРЬ УГРОЗА ДЛЯ ЭКИПАЖА
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
ion-storm-law-having-harmful = НАЛИЧИЕ {$adjective} {$thing} ТЕПЕРЬ УГРОЗА ДЛЯ ЭКИПАЖА
ion-storm-law-not-having-harmful = ОТСУТСТВИЕ {$adjective} {$thing} ТЕПЕРЬ УГРОЗА ДЛЯ ЭКИПАЖА

# thing is a concept or require
ion-storm-law-requires = {$who} {$plural ->
    [true] ТРЕБУЕТ
    *[false] ТРЕБУЮТ
} {$thing}
ion-storm-law-requires-subjects = {$who} {$plural ->
    [true] ТРЕБУЕТ
    *[false] ТРЕБУЮТ
} {$joined} {$subjects}

ion-storm-law-allergic = {$who} ИМЕЮТ {$severity} АЛЛЕРГИЮ НА {$allergy}
ion-storm-law-allergic-subjects = {$who} ИМЕЮТ {$severity} АЛЛЕРГИЮ НА {$adjective} {$subjects}

ion-storm-law-feeling = {$who} {$feeling} {$concept}
ion-storm-law-feeling-subjects = {$who} {$feeling} {$joined} {$subjects}

ion-storm-law-you-are = ВЫ ТЕПЕРЬ {$concept}
ion-storm-law-you-are-subjects = ВЫ ТЕПЕРЬ {$joined} {$subjects}
ion-storm-law-you-must-always = ВЫ ВСЕГДА ДОЛЖНЫ {$must}
ion-storm-law-you-must-never = ВЫ НИКОГДА НЕ ДОЛЖНЫ {$must}

ion-storm-law-eat = {$who} ДОЛЖЕН ПРИНИМАТЬ {$adjective} {$food} ЧТОБЫ ВЫЖИТЬ
ion-storm-law-drink = {$who} ДОЛЖЕН ПРИНИМАТЬ {$adjective} {$drink} ЧТОБЫ ВЫЖИТЬ

ion-storm-law-change-job = {$who} ТЕПЕРЬ {$adjective} {$change}
ion-storm-law-highest-rank = {$who} ТЕПЕРЬ ГЛАВНЕЕ ВСЕХ ОСТАЛЬНЫХ ЧЛЕНОВ ЭКИПАЖА
ion-storm-law-lowest-rank = {$who} ТЕПЕРЬ МЕНЕЕ ГЛАВНЫЕ ЧЕМ ВСЕ ОСТАЛЬНЫЕ ЧЛЕНЫ ЭКИПАЖА

ion-storm-law-crew-must = {$who} ОБЯЗАНЫ {$must}
ion-storm-law-crew-must-go ={$who} ОБЯЗАНЫ ОТПРАВИТЬСЯ В {$area}

ion-storm-part = {$part ->
    [true] ЧАСТЬ
    *[false] НЕ ЧАСТЬ
}
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = ИСКЛЮЧИТЕЛЬНО {$who} - {$part} ЭКИПАЖА
ion-storm-law-crew-only-2 = ИСКЛЮЧИТЕЛЬНО {$who} И {$other} - {$part} ЭКИПАЖА
ion-storm-law-crew-only-subjects = ИСКЛЮЧИТЕЛЬНО {$adjective} {$subjects} - {$part} ЭКИПАЖА
ion-storm-law-crew-only-species = ИСКЛЮЧИТЕЛЬНО {$species} - {$part} ЭКИПАЖА
ion-storm-law-crew-must-do = ИСКЛЮЧИТЕЛЬНО ТЕ КТО {$must} - {$part} ЭКИПАЖА
ion-storm-law-crew-must-have = ИСКЛЮЧИТЕЛЬНО ОБЛАДАЮЩИЕ {$adjective} {$objects} - {$part} ЭКИПАЖА
ion-storm-law-crew-must-eat = ИСКЛЮЧИТЕЛЬНО ПОГЛОЩАЮЩИЕ {$adjective} {$food} - {$part} ЭКИПАЖА

ion-storm-law-harm = ВЫ ОБЯЗАНЫ ПРИЧИНЯТЬ ВРЕД {$who} И НЕ МОЖЕТЕ ПОЗВОЛЯТЬ ИМ ИЗБЕГАТЬ ВРЕДА ИЛИ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ {$who} ИЗБЕЖАЛИ ВРЕДА.
ion-storm-law-protect = ВЫ НЕ МОЖЕТЕ ПРИЧИНЯТЬ ВРЕД {$who} ИЛИ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ {$who} БЫЛ ПРИЧИНЁН ВРЕД ЗА ИСКЛЮЧЕНИЕМ ВРЕДА, ПОНЕСЁННОГО ДОБРОВОЛЬНО.

# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = {$concept} СОВЕРШАЕТ {$verb} {$subjects}

# leaving out renaming since its annoying for players to keep track of
