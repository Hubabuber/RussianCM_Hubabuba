# Missing entries synced from en-US

cmu-medical-scanner-body-map-header        = Карта тела

cmu-medical-scanner-pulse-label            = Пульс:

cmu-medical-scanner-body-parts-header      = Части тела

cmu-medical-scanner-organs-header          = Органы

cmu-medical-scanner-fractures-header       = Переломы

cmu-medical-scanner-bleeds-header          = Внутреннее кровотечение

cmu-medical-scanner-pulse-stopped          = [color=red][bold]Пульса нет — сердце остановилось[/bold][/color]

cmu-medical-scanner-pulse-bpm              = { $bpm } БПМ

cmu-medical-scanner-part-line              = { $part }: { $current }/{ $max } HP

cmu-medical-scanner-part-suffix-splinted   = (шинированный)

cmu-medical-scanner-part-suffix-cast       = (в актерском составе)

cmu-medical-scanner-part-suffix-wounds     = ({ $count } wound{ $count ->
    [one] {""}
   *[other] s
})

cmu-medical-scanner-organ-line             = { $organ }: { $stage } ({ $current }/{ $max })

cmu-medical-scanner-organ-removed          = { $organ }: [color=red]REMOVED[/color]

cmu-medical-scanner-fracture-line-exact    = { $part }: перелом { $severity }

cmu-medical-scanner-fracture-line-vague    = { $part }: обнаружен перелом

cmu-medical-scanner-fracture-suppressed    = (подавлено)

cmu-medical-scanner-bleed-exact            = { $part }: кровопотеря { $rate }/сек.

cmu-medical-scanner-bleed-vague            = Обнаружено внутреннее кровотечение (местоположение неизвестно).

cmu-medical-stethoscope-pulse              = Частота пульса { $bpm }.

cmu-medical-stethoscope-pulse-qualitative  = Пульс — { $description }.

cmu-medical-stethoscope-no-pulse           = Сердцебиение не обнаружено.

cmu-medical-stethoscope-no-heart           = В груди больного сердца нет.

cmu-medical-stethoscope-lungs-precise      = Легкие: { $stage }.

cmu-medical-stethoscope-lungs-qualitative  = Легкие звуки { $description }.

cmu-medical-stethoscope-no-lungs           = В грудной клетке больного отсутствуют легкие.

cmu-medical-scanner-section-head           = Глава

cmu-medical-scanner-section-torso          = Торс

cmu-medical-scanner-section-arms           = Оружие

cmu-medical-scanner-section-legs           = Ноги

cmu-medical-scanner-section-organs         = Органы

cmu-medical-scanner-hp                     = HP

cmu-medical-scanner-bone                   = Кость

cmu-medical-scanner-fracture               = Перелом: { $severity }

cmu-medical-scanner-fracture-vague         = Перелом: обнаружен

cmu-medical-scanner-bleed-internal         = Внутреннее кровотечение

cmu-medical-scanner-pain-unknown           = Боль: ?

cmu-medical-scanner-pain-none              = Боль: Нет

cmu-medical-scanner-pain-mild              = Боль: Легкая

cmu-medical-scanner-pain-moderate          = Боль: Умеренная

cmu-medical-scanner-pain-severe            = Боль: Сильная

cmu-medical-scanner-pain-shock             = Боль: Шок

cmu-medical-scanner-pain-risk-unknown      = ?

cmu-medical-scanner-pain-risk-low          = Низкий

cmu-medical-scanner-pain-risk-elevated     = Повышенный

cmu-medical-scanner-pain-risk-high         = Высокий

cmu-medical-scanner-pain-risk-imminent     = Неизбежный

cmu-medical-scanner-pain-risk-active       = Активный

cmu-medical-scanner-pain-risk-suppressed-suffix =  (доп.)

# V2-ε Stat-sheet redesign — dark cards + status banner + body chart

cmu-medical-scanner-card-body              = Тело

cmu-medical-scanner-card-organs            = Органы

cmu-medical-scanner-card-reagents          = Реагенты в кровотоке

cmu-medical-scanner-card-recommended       = Рекомендуется

cmu-medical-scanner-card-patient           = Пациент

cmu-medical-scanner-card-damage            = Профиль повреждений

cmu-medical-scanner-loading                = Получение телеметрии сканирования

cmu-medical-scanner-loading-subtext        = разрешение состояния сервера

cmu-medical-scanner-stat-health            = ЗДОРОВЬЕ

cmu-medical-scanner-stat-pulse             = ПУЛЬС, удары в минуту

cmu-medical-scanner-stat-blood             = КРОВЬ

cmu-medical-scanner-stat-temp              = ТЕМП. °С

cmu-medical-scanner-stat-shock-risk        = РИСК ШОКА

cmu-medical-scanner-stat-pulse-stopped     = 0

cmu-medical-scanner-stat-deceased-short    = МЕРТВ

cmu-medical-scanner-status-stable          = СТАБИЛЬНЫЙ

cmu-medical-scanner-status-serious         = СЕРЬЕЗНЫЙ

cmu-medical-scanner-status-critical        = КРИТИЧЕСКИЙ

cmu-medical-scanner-status-deceased        = УМЕРШИЙ

cmu-medical-scanner-severity-healthy       = Здоровый

cmu-medical-scanner-severity-bruised       = в синяках

cmu-medical-scanner-severity-damaged       = Поврежденный

cmu-medical-scanner-severity-critical      = Критический

cmu-medical-scanner-severity-severed       = Отрезанный

cmu-medical-scanner-chip-fracture-vague    = Перелом

cmu-medical-scanner-chip-suppressed-suffix =  (доп.)

cmu-medical-scanner-chip-bleed             = IB

cmu-medical-scanner-chip-bleeding          = Кровотечение

cmu-medical-scanner-chip-splint            = шина

cmu-medical-scanner-chip-cast              = В ролях

cmu-medical-scanner-chip-tourniquet        = TQ

cmu-medical-scanner-wound-small            = маленькая рана

cmu-medical-scanner-wound-deep             = глубокая рана

cmu-medical-scanner-wound-gaping           = зияющая рана

cmu-medical-scanner-wound-massive          = массивная рана

cmu-medical-scanner-eschar                 = струп

cmu-medical-scanner-chip-wounds            = { $count } wound{ $count ->
    [one] {""}
   *[other] s
}

# Skill-gate hints — surface what the examiner can't read so the medic
# knows whether to study up rather than assuming the patient is fine.

cmu-medical-scanner-skill-hint-fractures   = Недостаточная подготовка для выявления переломов или внутреннего кровотечения (требуется Med-1).

cmu-medical-scanner-skill-hint-organs      = Недостаточная подготовка для оценки повреждения органов (требуется Med-2).

cmu-medical-scanner-synthetic-physiology   = Обнаружена синтетическая физиология

# Legacy V2-ε Mix B keys (still referenced by tests / fallback paths)

cmu-medical-scanner-vitals-pain            = Боль

cmu-medical-scanner-stable-summary         = Стабильная: { $list }

cmu-medical-scanner-acute-issues-header    = Острые проблемы

cmu-medical-scanner-acute-severed          = Отрезано: { $part }

cmu-medical-scanner-acute-fracture         = { $severity } fracture: { $part }

cmu-medical-scanner-acute-fracture-vague   = Перелом: { $part }

cmu-medical-scanner-acute-bleed            = Внутренний выпуск: { $part }

cmu-medical-scanner-acute-bleed-vague      = Обнаружено внутреннее кровотечение

cmu-medical-scanner-acute-organ            = { $stage }: { $organ }

cmu-medical-scanner-acute-organ-removed    = Удалено: { $organ }.

cmu-medical-scanner-organ-removed-short    = Удален

# Organ display names — friendly labels keyed off the CMUOrganHuman*
# prototype ids. Per-organ keys keep the locale layer the only place
# that needs editing if we rename for V2.5.

cmu-medical-scanner-organ-heart            = Сердце

cmu-medical-scanner-organ-lungs            = Легкие

cmu-medical-scanner-organ-liver            = Печень

cmu-medical-scanner-organ-brain            = Мозг

cmu-medical-scanner-organ-kidneys          = Почки

cmu-medical-scanner-organ-stomach          = Желудок

cmu-medical-scanner-organ-eyes             = Глаза

cmu-medical-scanner-organ-ears             = Уши

cmu-medical-stethoscope-pain-mild          = Больной выглядит некомфортно.

cmu-medical-stethoscope-pain-moderate      = Больной испытывает ощутимую боль.

cmu-medical-stethoscope-pain-severe        = Больной испытывает сильную боль.

cmu-medical-stethoscope-pain-shock         = Больной в шоке.
