# TODO: заменить на genitive там где возможно
health-examinable-carbon-none = Видимые повреждения тела отсутствуют.
health-examinable-carbon-Slash-8 = [color=yellow]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } неглубокий порез.[/color]
health-examinable-carbon-Slash-15 = [color=yellow]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } несколько маленьких порезов.[/color]
health-examinable-carbon-Slash-30 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } несколько значительных порезов.[/color]
health-examinable-carbon-Slash-50 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто глубокими порезами.[/color]
health-examinable-carbon-Slash-75 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто глубокими рваными ранами.[/color]
health-examinable-carbon-Slash-100 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело выглядит окровавленным и израненным.[/color]
health-examinable-carbon-Slash-200 = [color=crimson]{ CAPITALIZE(POSS-ADJ($target)) } тело полностью разорвано на куски![/color]
health-examinable-carbon-Blunt-8 = [color=yellow]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } лёгкий ушиб.[/color]
health-examinable-carbon-Blunt-15 = [color=yellow]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } несколько ушибов.[/color]
health-examinable-carbon-Blunt-30 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { GENDER($target) ->
        [male] избит
        [female] избита
        [epicene] избиты
       *[neuter] избито
    }.[/color]
health-examinable-carbon-Blunt-50 = [color=red]{ CAPITALIZE(SUBJECT($target)) } сильно { GENDER($target) ->
        [male] избит
        [female] избита
        [epicene] избиты
       *[neuter] избито
    }.[/color]
health-examinable-carbon-Blunt-75 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто серьёзными тупыми травмами.[/color]
health-examinable-carbon-Blunt-100 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело обезображено и сильно избито.[/color]
health-examinable-carbon-Blunt-200 = [color=crimson]{ CAPITALIZE(POSS-ADJ($target)) } тело разбито в лепёшку![/color]
health-examinable-carbon-Piercing-8 = [color=yellow]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } небольшую колотую рану.[/color]
health-examinable-carbon-Piercing-15 = [color=yellow]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } несколько колотых ран.[/color]
health-examinable-carbon-Piercing-30 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } множественные глубокие колотые раны.[/color]
health-examinable-carbon-Piercing-50 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто серьёзными глубокими проколами.[/color]
health-examinable-carbon-Piercing-75 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто обширными глубокими разрывами тканей.[/color]
health-examinable-carbon-Piercing-100 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело полностью покрыто огромными, зияющими дырами.[/color]
health-examinable-carbon-Piercing-200 = [color=crimson]{ CAPITALIZE(POSS-ADJ($target)) } тело выглядит разорванным![/color]
health-examinable-carbon-Asphyxiation-30 = [color=lightblue]{ CAPITALIZE(POSS-ADJ($target)) } губы синеют.[/color]
health-examinable-carbon-Asphyxiation-75 = [color=lightblue]{ CAPITALIZE(POSS-ADJ($target)) } лицо синеет.[/color]
health-examinable-carbon-Heat-8 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } поверхностный ожог.[/color]
health-examinable-carbon-Heat-15 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } несколько ожогов первой степени.[/color]
health-examinable-carbon-Heat-30 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } несколько ожогов второй степени.[/color]
health-examinable-carbon-Heat-50 = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { GENDER($target) ->
        [male] покрыт
        [female] покрыта
        [epicene] покрыты
       *[neuter] покрыто
    } ожогами второй степени.[/color]
health-examinable-carbon-Heat-75 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто тяжёлыми ожогами третьей степени.[/color]
health-examinable-carbon-Heat-100 = [color=red]{ CAPITALIZE(POSS-ADJ($target)) } тело значительно покрыто ожогами четвёртой степени.[/color]
health-examinable-carbon-Heat-200 = [color=crimson]{ CAPITALIZE(POSS-ADJ($target)) } тело полностью обуглено![/color]
health-examinable-carbon-Shock-15 = [color=lightgoldenrodyellow]На { POSS-ADJ($target) } коже лёгкие следы обугливания.[/color]
health-examinable-carbon-Shock-30 = [color=lightgoldenrodyellow]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто следами обугливания.[/color]
health-examinable-carbon-Shock-50 = [color=lightgoldenrodyellow]{ CAPITALIZE(POSS-ADJ($target)) } тело серьёзно обуглено.[/color]
health-examinable-carbon-Shock-75 = [color=lightgoldenrodyellow]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто большими обугленными ранами.[/color]
health-examinable-carbon-Shock-100 = [color=lightgoldenrodyellow]Всё { POSS-ADJ($target) } покрыто сильными электрическими ожогами![/color]
health-examinable-carbon-Shock-200 = [color=lightgoldenrodyellow]{ CAPITALIZE(POSS-ADJ($target)) } тело полностью зажарено![/color]
health-examinable-carbon-Cold-8 = [color=lightblue]На кончиках { POSS-ADJ($target) } пальцев лёгкие обморожения.[/color]
health-examinable-carbon-Cold-15 = [color=lightblue]На кончиках { POSS-ADJ($target) } конечностей обморожения первой степени.[/color]
health-examinable-carbon-Cold-30 = [color=lightblue]На { POSS-ADJ($target) } конечностях обморожения второй степени.[/color]
health-examinable-carbon-Cold-50 = [color=lightblue]На { POSS-ADJ($target) } конечностях сильные обморожения третьей степени.[/color]
health-examinable-carbon-Cold-75 = [color=lightblue]{ CAPITALIZE(POSS-ADJ($target)) } конечности тёмные, холодные и омертвевшие.[/color]
health-examinable-carbon-Cold-100 = [color=lightblue]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто обширными обморожениями четвёртой степени.[/color]
health-examinable-carbon-Cold-200 = [color=lightblue]{ CAPITALIZE(POSS-ADJ($target)) } тело превратилось в ледышку![/color]
health-examinable-carbon-Caustic-8 = [color=yellowgreen]{ CAPITALIZE(POSS-ADJ($target)) } кожа выглядит немного обесцвеченной.[/color]
health-examinable-carbon-Caustic-15 = [color=yellowgreen]{ CAPITALIZE(POSS-ADJ($target)) } кожа выглядит раздражённой и обесцвеченной.[/color]
health-examinable-carbon-Caustic-30 = [color=yellowgreen]{ CAPITALIZE(POSS-ADJ($target)) } кожа воспалена и начинает шелушиться.[/color]
health-examinable-carbon-Caustic-50 = [color=yellowgreen]{ CAPITALIZE(POSS-ADJ($target)) } кожа обожжена и отслаивается большими кусками.[/color]
health-examinable-carbon-Caustic-75 = [color=yellowgreen]{ CAPITALIZE(POSS-ADJ($target)) } кожа сильно обожжена и отслаивается.[/color]
health-examinable-carbon-Caustic-100 = [color=yellowgreen]{ CAPITALIZE(POSS-ADJ($target)) } тело покрыто сильными химическими ожогами.[/color]
health-examinable-carbon-Caustic-200 = [color=yellowgreen]Большая часть { POSS-ADJ($target) } тела полностью расплавлена![/color]
health-examinable-carbon-Radiation-50 = [color=orange]На { POSS-ADJ($target) } коже образовались большие волдыри.[/color]
health-examinable-carbon-Radiation-100 = [color=orange]{ CAPITALIZE(POSS-ADJ($target)) } кожа покрыта язвами и отслаивается кусками.[/color]
