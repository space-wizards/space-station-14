## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая задача: Выжить
survivor-role-greeting =
    Вы — Выживший.
    Прежде всего, вам нужно добраться до Центральгого Командования живым.
    Соберите столько огневой мощи, сколько потребуется для гарантии вашего выживания.
    Никому не доверяйте.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший погиб.
       *[other] [color=red]{ $deadCount }[/color] выживших погибло.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший был оставлен на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выжившие были оставлены на станции.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший смог выбраться живым.
       *[other] [color=green]{ $aliveCount }[/color] выжившие смогли выбраться живыми.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация Космических Магов[/color]
wizard-title = Маг
wizard-description = На станции появился маг! Никогда не знаешь, что он может сделать.
roles-antag-wizard-name = Маг
roles-antag-wizard-objective = Преподать им урок, который они никогда не забудут.
wizard-role-greeting =
    ТЫ — МАГ!
    Между Федерацией Космических Магов и NanoTrasen возникли трения.
    Поэтому федерация выбрала вас, чтобы вы навестили станцию.
    Продемонстрируйте им всю мощь своих способностей.
    Что вы будете делать, зависит от вас, но помните: федерация хочет, чтобы вы остались в живых.
wizard-round-end-name = маг

## TODO: Wizard Apprentice (Coming sometime post-wizard release)

