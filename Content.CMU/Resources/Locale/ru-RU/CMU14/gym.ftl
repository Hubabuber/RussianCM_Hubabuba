tiles-cmu-arena = пол арены

tiles-cmu-arena-tile = плитка арены

cmu-treadmill-in-use = На {THE($treadmill)} уже кто-то бежит.

cmu-exercise-start-machine = занимает {THE($machine)} и начинает тренировку.

cmu-exercise-start-curls = начинает поднимать {THE($dumbbell)} на бицепс.

cmu-exercise-start-treadmill = встаёт на {THE($treadmill)} и начинает бежать.

cmu-stacklifter-count = выполняет {$count} {$count ->
    [one] повторение
    [few] повторения
   *[other] повторений
} на силовом тренажёре.

cmu-stacklifter-fail-emote = отпускает груз, больше не в силах продолжать после {$count} {$count ->
    [one] повторения
   *[other] повторений
}!

cmu-stacklifter-fail = Руки отказывают! Вы выполнили {$count} {$count ->
    [one] повторение
    [few] повторения
   *[other] повторений
}. Вы чувствуете себя сильнее!

cmu-stacklifter-stop = Вы прекращаете поднимать груз после {$count} {$count ->
    [one] повторения
   *[other] повторений
}.

cmu-benchpress-count = выполняет {$count} {$count ->
    [one] повторение
    [few] повторения
   *[other] повторений
} жима лёжа.

cmu-benchpress-fail-emote = возвращает штангу на стойку, больше не в силах выжать её после {$count} {$count ->
    [one] повторения
   *[other] повторений
}!

cmu-benchpress-fail = Грудные мышцы отказывают! Вы выполнили {$count} {$count ->
    [one] повторение
    [few] повторения
   *[other] повторений
}.

cmu-benchpress-stop = Вы возвращаете штангу на стойку после {$count} {$count ->
    [one] повторения
   *[other] повторений
}.

cmu-curls-count = выполняет {$count} {$count ->
    [one] подъём
    [few] подъёма
   *[other] подъёмов
} на бицепс.

cmu-curls-fail-emote = опускает гантель, больше не в силах поднять её после {$count} {$count ->
    [one] повторения
   *[other] повторений
}!

cmu-curls-fail = Бицепсы отказывают! Вы выполнили {$count} {$count ->
    [one] подъём
    [few] подъёма
   *[other] подъёмов
}.

cmu-curls-popup-self = Вы выполняете подъём на бицепс № {$ordinal}.

cmu-curls-popup-others = {CAPITALIZE(THE($user))} выполняет {POSS-ADJ($user)} подъём на бицепс № {$ordinal}.

cmu-curls-stop = Вы прекращаете подъёмы после {$count} {$count ->
    [one] повторения
   *[other] повторений
}.

cmu-punching-count = наносит {$count} ударов.

cmu-punching-fail-emote = обессиленно опирается на боксёрскую грушу после {$count} {$count ->
    [one] удара
   *[other] ударов
}!

cmu-punching-fail = Вы выдохлись! Вы нанесли {$count} {$count ->
    [one] удар
    [few] удара
   *[other] ударов
}. Рукам нужен отдых.

cmu-punching-tired = Руки слишком устали для ударов. Дайте им отдохнуть.

cmu-running-count = бежит уже {$count} секунд.

cmu-running-fail-emote = падает на беговую дорожку без сил после {$count} {$count ->
    [one] секунды
   *[other] секунд
} бега!

cmu-running-fail = Ноги отказывают! Вы бежали {$count} {$count ->
    [one] секунду
    [few] секунды
   *[other] секунд
}.

cmu-running-stop = Вы сходите с беговой дорожки после {$count} {$count ->
    [one] секунды
   *[other] секунд
} бега.

ent-CMUFitnessWeightsBench = силовой тренажёр
    .desc = Даже один взгляд на эту штуку вызывает усталость.

ent-CMUFitnessWeightsBenchUnanchored =
    .suffix = Не закреплён
    .desc = { ent-CMUFitnessWeightsBench.desc }

ent-CMUFitnessWeightLifter = силовой тренажёр
    .desc = Даже один взгляд на эту штуку вызывает усталость.

ent-CMUFitnessWeightLifterUnanchored =
    .suffix = Не закреплён
    .desc = { ent-CMUFitnessWeightLifter.desc }

ent-CMUFitnessPunchingBag = боксёрская груша
    .desc = Боксёрская груша. Сможете достичь четвёртого уровня скорости??? Бейте её, чтобы тренироваться.

ent-CMUTreadmillBase = беговая дорожка
    .desc = Беговая дорожка для кардиотренировок.

ent-CMUTreadmill =
    .suffix = Восток
    .desc = { ent-CMUTreadmillBase.desc }

ent-CMUTreadmillInverted =
    .suffix = Запад
    .desc = { ent-CMUTreadmillBase.desc }

ent-CMUTreadmillHandles = поручни беговой дорожки
    .desc = { "" }

ent-CMUDumbbell = тяжёлая гантель
    .desc = Поднимайте её для силы или уроните на палец ноги для мгновенного просветления. В любом случае результат гарантирован.

ent-CMUDumbbellAlt = лёгкая гантель
    .desc = Гантель полегче, для большего числа повторений. Уронить её на палец ноги всё ещё больно.

ent-CMURingRopes = канаты ринга
    .desc = Канаты боксёрского ринга. Берегите голову, когда заходите.

ent-CMUMetalPost = металлический столб
    .desc = Простой металлический столб.

ent-CMUPosterUSCMFaded = плакат КМП США
    .desc = Символ Колониального корпуса морской пехоты США, выцветший со временем.

ent-CMUPosterUSCMWorn = потрёпанный плакат КМП США
    .desc = Символ Колониального корпуса морской пехоты США, сильно потрёпанный и выцветший со временем.
