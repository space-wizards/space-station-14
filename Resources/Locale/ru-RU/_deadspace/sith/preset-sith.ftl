roles-antag-sith-name = Ренегат
roles-antag-sith-objective = С помощью силы вы можете уничтожить импланты защиты разума. Подчините кмандование станции.
sith-break-control =
    { $name } { $gender ->
        [male] вспомнил, кому он верен
        [female] вспомнила, кому она верна
        [epicene] вспомнили, кому они верни
       *[neuter] вспомнило, кому оно верно
    } на самом деле!
sith-sub-name-user = [color=#5e9cff]{ $name }[/color] ([color=gray]{ $username }[/color]) поработил { $count } { $count ->
        [one] члена
       *[other] членов
    } командования
sith-round-end-agent-name = Ренегат
objective-issuer-sith = [color=#ff0000]Орден Ностор[/color]
sith-role-greeting =
    Я — верный слуга бога Ренды, воин ордена Ностор.
    Гнев — моё оружие. Сила — моя броня. Без них я ничто.
    Захватите станцию, подчиняя глав с помощью силы, после вызовете шаттл эвакуации. 
    Очистись или умри, пытаясь!
objective-condition-subordination-command-title =
    Подчинить { $count } { $count ->
        [one] главу
        [few] глав
       *[other] глав
    }.
round-end-system-sith-shuttle-called-announcement = Внимание, всему персоналу! Станция перешла под контроль ордена Ностор. Немедленно покиньте станцию на эвакуационном шаттле. При невозможности эвакуарироваться, соблюдайте все требования захватчиков. NanoTrasen уже ведёт переговоры о вашем освобождении.
