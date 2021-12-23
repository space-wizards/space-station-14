
## Traitor

# Shown at the end of a round of Traitor
traitor-round-end-result = {$traitorCount ->
    [one] Был один предатель.
    *[other] Было {$traitorCount} предателей.
}

# Shown at the end of a round of Traitor
traitor-user-was-a-traitor = [color=gray]{$user}[/color] был(а) предателем.
traitor-user-was-a-traitor-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) был(а) предателем.
traitor-was-a-traitor-named = [color=White]{$name}[/color] был(а) предателем.

traitor-user-was-a-traitor-with-objectives = [color=gray]{$user}[/color] был(а) предателем, у которого(-ой) были следующие цели:
traitor-user-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) был(а) предателем, у которого(-ой) были следующие цели:
traitor-was-a-traitor-with-objectives-named = [color=White]{$name}[/color] был(а) предателем, у которого(-ой) были следующие цели:

preset-traitor-objective-issuer-syndicate = [color=#87cefa]Синдикат[/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-success = {$condition} | [color={$markupColor}]Успешно![/color]

# Shown at the end of a round of Traitor
traitor-objective-condition-fail = {$condition} | [color={$markupColor}]Провал![/color] ({$progress}%)

traitor-title = Предатель
traitor-description = Среди нас есть предатели...
traitor-not-enough-ready-players = Недостаточно игроков готовы к игре! Из {$minimumPlayers} необходимых игроков готово {$readyPlayersCount}.
traitor-no-one-ready = Нет готовых игроков! Не удалось начать режим Предатель.

## TraitorDeathMatch
traitor-death-match-title = DeathMatch Предателей
traitor-death-match-description = Все - предатели. Все хотят смерти друг друга.
traitor-death-match-station-is-too-unsafe-announcement = Станция слишком опасна для продолжения. У вас есть одна минута.
traitor-death-match-end-round-description-first-line = После этого КПК восстановились...
traitor-death-match-end-round-description-entry = КПК {$originalName}, с {$tcBalance} TC

## TraitorRole

# TraitorRole
traitor-role-name = Агент синдиката
traitor-role-greeting = Здравствуйте, агент
traitor-role-codewords = Ваши кодовые слова: {$codewords}
