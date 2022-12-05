## Traitor

# Shown at the end of a round of Traitor
traitor-round-end-result =
    { $traitorCount ->
        [one] Был один предатель.
       *[other] Было { $traitorCount } предателей.
    }
# Shown at the end of a round of Traitor
traitor-user-was-a-traitor = [color=gray]{ $user }[/color] был(а) предателем.
traitor-user-was-a-traitor-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color]) был(а) предателем.
traitor-was-a-traitor-named = [color=White]{ $name }[/color] был(а) предателем.
traitor-user-was-a-traitor-with-objectives = [color=gray]{ $user }[/color] был(а) предателем, у которого(-ой) были следующие цели:
traitor-user-was-a-traitor-with-objectives-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color]) был(а) предателем, у которого(-ой) были следующие цели:
traitor-was-a-traitor-with-objectives-named = [color=White]{ $name }[/color] был(а) предателем, у которого(-ой) были следующие цели:
preset-traitor-objective-issuer-syndicate = [color=#87cefa]Синдикат[/color]
# Shown at the end of a round of Traitor
traitor-objective-condition-success = { $condition } | [color={ $markupColor }]Успех![/color]
# Shown at the end of a round of Traitor
traitor-objective-condition-fail = { $condition } | [color={ $markupColor }]Провал![/color] ({ $progress }%)
traitor-title = Предатель
traitor-description = Среди нас есть предатели...
traitor-not-enough-ready-players = Недостаточно игроков готовы к игре! Из { $minimumPlayers } необходимых игроков готовы { $readyPlayersCount }.
traitor-no-one-ready = Нет готовых игроков! Не удалось начать режим Предателя.

## TraitorDeathMatch

traitor-death-match-title = Бой насмерть предателей
traitor-death-match-description = Все — предатели. Все хотят смерти друг друга.
traitor-death-match-station-is-too-unsafe-announcement = На станции слишком опасно, чтобы продолжать. У вас есть одна минута.
traitor-death-match-end-round-description-first-line = КПК были восстановлены...
traitor-death-match-end-round-description-entry = КПК { $originalName }, с { $tcBalance } ТК

## TraitorRole

# TraitorRole
traitor-role-greeting =
    Вы - агент Синдиката.
    Ваши цели и кодовые слова перечислены в меню персонажа.
    Воспользуйтесь аплинком, встроенным в ваш КПК, чтобы приобрести всё необходимое для выполнения работы.
    Смерть Nanotrasen!
traitor-role-codewords =
    Кодовые слова следующие:
    { $codewords }
    Кодовые слова можно использовать в обычном разговоре, чтобы незаметно идентифицировать себя для других агентов Синдиката.
    Прислушивайтесь к ним и храните их в тайне.
