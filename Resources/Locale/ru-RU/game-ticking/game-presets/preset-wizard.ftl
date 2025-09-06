## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая задача: Выжить
survivor-role-greeting =
    Вы — выживший.
    Ваша главная задача остаться в живых и вернуться на Центком.
    Никому не доверяйте.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший умер.
       *[other] [color=red]{ $deadCount }[/color] выживших умерло.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший остался на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выживших осталось на станции.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший выбрался живым.
       *[other] [color=green]{ $aliveCount }[/color] выживших выбралось живыми.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация Космических Магов[/color]
wizard-title = Маг
wizard-description = На станции присутствует маг! Никогда не знаешь, что они могут натворить.
roles-antag-wizard-name = Маг
roles-antag-wizard-objective = Преподайте им урок, который они никогда не забудут.
wizard-role-greeting =
    ТЫ — МАГ!
    Между Федерацией Космических Магов и NanoTrasen возникли трения.
    Поэтому федерация выбрала вас, чтобы вы навестили станцию.
    Хорошенько продемонстрируйте им свои способности.
    Вам решать, что именно предпринять, но помните, что Космические Маги желают, чтобы вы вернулись живыми.
wizard-round-end-name = маг

## TODO: Wizard Apprentice (Coming sometime post-wizard release)
