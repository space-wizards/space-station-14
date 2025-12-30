set-game-preset-command-description = Установить игровой пресет для указанного количества предстоящих раундов. Может отображать имя и описание другого пресета, чтобы обмануть игроков.
set-game-preset-command-help-text = setgamepreset <id> [количество раундов, по умолчанию 1]
set-game-preset-command-hint-1 = <id>
set-game-preset-command-hint-2 = [количество раундов]
set-game-preset-command-hint-3 = [пресет для обмана]
set-game-preset-optional-argument-not-integer = Если второй аргумент предоставлен, он должен быть числом.
set-game-preset-preset-error = Не удаётся найти игровой пресет "{ $preset }"
set-game-preset-decoy-error = Если третий аргумент предоставлен, он должен быть валидным пресетом. Не удаётся найти игровой пресет "{ $preset }"
#set-game-preset-preset-set = Установлен пресет "{ $preset }"
set-game-preset-preset-set-finite =
    Установлен пресет "{ $preset }" на { $rounds ->
        [one] следующий раунд
        [few] следующие { $rounds } раунда
       *[other] следующие { $rounds } раундов
    }.
set-game-preset-preset-set-finite-with-decoy =
    Установлен пресет "{ $preset }" на { $rounds ->
        [one] следующий раунд
        [few] следующие { $rounds } раунда
       *[other] следующие { $rounds } раундов
    }, отображая в лобби { $decoy }.
