agent-id-new =
    { CAPITALIZE($card) } { $number ->
        [0] не дала новых доступов
        [one] дала { $number } новый доступ
        [few] дала { $number } новых доступа
       *[other] дала { $number } новых доступов
    }.
agent-id-card-current-name = Имя:
agent-id-card-current-job = Должность:
agent-id-card-job-icon-label = Иконка:
agent-id-menu-title = ID карта Агента
