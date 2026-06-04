cmu-medical-examine-wound-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $wounds } на { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-fracture-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } { $fracture } в { POSS-ADJ($target) } { $part }.[/color]
cmu-medical-examine-wounds-line = [color=red]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } раны: { $parts }.[/color]
cmu-medical-examine-fractures-line = [color=#dca94c]{ CAPITALIZE(SUBJECT($target)) } { CONJUGATE-HAVE($target) } переломы: { $parts }.[/color]
cmu-medical-examine-body-part-line = { $part }: { $conditions }.

cmu-medical-examine-wound-size-small = небольшая
cmu-medical-examine-wound-size-deep = глубокая
cmu-medical-examine-wound-size-deep-visible = умеренная
cmu-medical-examine-wound-size-gaping = зияющая
cmu-medical-examine-wound-size-gaping-visible = большая
cmu-medical-examine-wound-size-massive = массивная

cmu-medical-examine-wound-type-burn = ожоговая рана
cmu-medical-examine-wound-type-wound = рана
cmu-medical-examine-wound-type-surgery = хирургическая рана
cmu-medical-examine-wound-type-trauma = травматическая рана

cmu-medical-examine-wound-treated-prefix = обработанная
cmu-medical-examine-wound-bleeding-suffix = (кровоточит)
cmu-medical-examine-wound-bleeding-active = активное кровотечение

cmu-medical-examine-wound-visible = { $treated ->
    [true] обработанная { $size } { $type }
   *[other] { $size } { $type }
}

cmu-medical-examine-fracture-hairline = { $stabilized ->
    [true]  стабилизированная трещина кости
   *[other] трещина кости
}
cmu-medical-examine-fracture-simple = { $stabilized ->
    [true]  стабилизированный перелом кости
   *[other] перелом кости
}
cmu-medical-examine-fracture-compound = { $stabilized ->
    [true]  стабилизированный открытый перелом
   *[other] открытый перелом
}
cmu-medical-examine-fracture-comminuted = { $stabilized ->
    [true]  стабилизированный оскольчатый перелом
   *[other] оскольчатый перелом
}

cmu-medical-examine-eschar = обугленная ткань

cmu-medical-examine-part-head = Голова
cmu-medical-examine-part-torso = Торс
cmu-medical-examine-part-arm-left = Левая рука
cmu-medical-examine-part-arm-right = Правая рука
cmu-medical-examine-part-hand-left = Левая кисть
cmu-medical-examine-part-hand-right = Правая кисть
cmu-medical-examine-part-leg-left = Левая нога
cmu-medical-examine-part-leg-right = Правая нога
cmu-medical-examine-part-foot-left = Левая стопа
cmu-medical-examine-part-foot-right = Правая стопа
cmu-medical-examine-part-severed = ОТСЕЧЕНА

cmu-medical-examine-list-and =  и
cmu-medical-examine-list-comma-and = { $list } и { $last }

cmu-medical-detailed-examine-verb = Осмотреть раны
cmu-medical-detailed-examine-verb-message = Внимательно осмотреть их раны.
cmu-medical-detailed-examine-start = Вы начинаете осматривать { THE($target) } на наличие ран.
cmu-medical-detailed-examine-none = Видимых ран не обнаружено.

cmu-medical-detailed-wound-full = { $mechanism ->
    [burn] { $size ->
        [small]   небольшой ожог
        [deep]    глубокий ожог
        [gaping]  обширный ожог
        [massive] массивный ожог
       *[other]   ожог
    }
    [bullet] { $size ->
        [small]   небольшое огнестрельное ранение
        [deep]    глубокое огнестрельное ранение
        [gaping]  зияющее огнестрельное ранение
        [massive] массивное огнестрельное ранение
       *[other]   огнестрельное ранение
    }
    [stab] { $size ->
        [small]   небольшое колотое ранение
        [deep]    глубокое колотое ранение
        [gaping]  зияющее колотое ранение
        [massive] массивное колотое ранение
       *[other]   колотое ранение
    }
    [slash] { $size ->
        [small]   небольшое резаное ранение
        [deep]    глубокое резаное ранение
        [gaping]  зияющее резаное ранение
        [massive] массивное резаное ранение
       *[other]   резаное ранение
    }
    [crush] { $size ->
        [small]   небольшое размозжённое ранение
        [deep]    глубокое размозжённое ранение
        [gaping]  зияющее размозжённое ранение
        [massive] массивное размозжённое ранение
       *[other]   размозжённое ранение
    }
    [blast] { $size ->
        [small]   небольшое взрывное ранение
        [deep]    глубокое взрывное ранение
        [gaping]  зияющее взрывное ранение
        [massive] массивное взрывное ранение
       *[other]   взрывное ранение
    }
    [fragment] { $size ->
        [small]   небольшое осколочное ранение
        [deep]    глубокое осколочное ранение
        [gaping]  зияющее осколочное ранение
        [massive] массивное осколочное ранение
       *[other]   осколочное ранение
    }
    [surgical] { $size ->
        [small]   небольшое хирургическое ранение
        [deep]    глубокое хирургическое ранение
        [gaping]  зияющее хирургическое ранение
        [massive] массивное хирургическое ранение
       *[other]   хирургическое ранение
    }
   *[other] { $size ->
        [small]   небольшое ранение
        [deep]    глубокое ранение
        [gaping]  зияющее ранение
        [massive] массивное ранение
       *[other]   ранение
    }
}

cmu-medical-detailed-treatment-optimal = оптимальное лечение
cmu-medical-detailed-treatment-adequate = достаточное лечение
cmu-medical-detailed-treatment-treated = обработано
cmu-medical-detailed-treatment-untreated = не обработано

cmu-medical-detailed-cleanup-retained-fragment = осколки в ране
cmu-medical-detailed-cleanup-poor-closure = плохое закрытие
cmu-medical-detailed-cleanup-charred-tissue = обугленная ткань
cmu-medical-detailed-cleanup-crush-debris = загрязнение от размозжения
cmu-medical-detailed-cleanup-dirty-dressing = загрязнённая повязка
cmu-medical-detailed-cleanup-needed = требуется обработка: { $items }

cmu-medical-detailed-hint-remove-shrapnel = удалить осколки
cmu-medical-detailed-hint-sealing-dressing = герметизирующая повязка
cmu-medical-detailed-hint-burn-gel-dressing = повязка с гелем от ожогов
cmu-medical-detailed-hint-compression-dressing = компрессионная повязка
cmu-medical-detailed-hint-hemostatic-dressing = гемостатическая повязка
cmu-medical-detailed-hint-antiseptic-dressing = антисептическая повязка
cmu-medical-detailed-hint-label = оптимально: { $hint }

cmu-medical-detailed-bleed-minor = незначительное
cmu-medical-detailed-bleed-moderate = умеренное
cmu-medical-detailed-bleed-severe = сильное
cmu-medical-detailed-bleed-arterial = артериальное

cmu-medical-detailed-external-bleeding = внешнее кровотечение: { $tier }
cmu-medical-detailed-eschar = ожоговый струп: обугленная ткань
cmu-medical-detailed-severed = ОТСЕЧЕНА
