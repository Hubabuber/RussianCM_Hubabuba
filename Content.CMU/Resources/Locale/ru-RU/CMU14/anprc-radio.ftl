anprc-window-title = Тактическая радиостанция AN/PRC-117G

anprc-transmit-hint-header = ПЕРЕДАЧА
anprc-transmit-hint-active = :r передаёт по активной предустановке

anprc-power-off-button = ВЫКЛЮЧИТЬ
anprc-power-on-button = ВКЛЮЧИТЬ

anprc-status-equipped = НАДЕТА
anprc-status-unequipped = НЕ НАДЕТА
anprc-status-on = ВКЛ.
anprc-status-off = ВЫКЛ.

anprc-slot-empty-display = НЕТ КАНАЛА


anprc-radio-off = Радиостанция не издаёт ни звука. Она выключена.
anprc-not-authorized = Радиостанция щёлкает. Вы не обучены работе с этим оборудованием.
anprc-no-active-slot = Ни одна предустановка не активна. Сначала добавьте радиосеть.
anprc-slot-empty = Предустановке { $slot } не назначен канал.
anprc-no-tower = Помехи. Для { $channel } требуется вышка связи или квалифицированный ретранслятор RTO.
anprc-not-rto-warning = Вы не обучены работе с этой радиостанцией. Она не будет ретранслировать радиосети у вас за спиной, и вы не сможете передавать.
anprc-verb-open = Открыть панель радиостанции
anprc-out-of-range = Помехи. В радиусе действия нет ретранслятора для { $channel }.

anprc-frequency-invalid = Неверный формат частоты. Введите число, например 1606 или 1.606.
anprc-frequency-out-of-band = На этой частоте радиосеть работать не может. Прямые частоты находятся в диапазоне 1.000–2.999 МГц или в softwave-диапазоне 30.000–87.999 МГц.
anprc-frequency-not-found = На частоте { $freq } канал не найден.
anprc-frequency-set = [{ $slot }] настроена на { $freq } МГц.
anprc-frequency-set-net = [{ $slot }] настроена на { $freq } МГц — радиосеть { $channel }.
anprc-frequency-set-unknown = [{ $slot }] настроена на { $freq } МГц — неопознанная радиосеть. Радиообмен будет записываться.
anprc-frequency-set-dynamic = [{ $slot }] установлена на { $freq } МГц (прямая частота) — радиосеть не назначена. Для передачи используйте :r.

anprc-frequency-card-fallback =
    {"["}head=2]ИНСТРУКЦИЯ ПО ОРГАНИЗАЦИИ СВЯЗИ[/head]
    {"["}head=3]НАЗНАЧЕНИЕ ЧАСТОТ РАДИОСЕТЕЙ AN/PRC-117G[/head]

    {"["}bold]Командование GOVFOR[/bold] - 2.592 МГц
    {"["}bold]GOVFOR Альфа[/bold]      - 2.502 МГц
    {"["}bold]GOVFOR Браво[/bold]      - 1.606 МГц
    {"["}bold]GOVFOR Чарли[/bold]      - 1.607 МГц
    {"["}bold]Разведка GOVFOR[/bold]   - 1.605 МГц
    {"["}bold]JTAC GOVFOR[/bold]       - 2.598 МГц
    {"["}bold]MILP GOVFOR[/bold]       - 2.595 МГц

    Введите частоту во вкладке FREQ панели радиостанции (с точкой или без неё: 2592 и 2.592 означают одно и то же), чтобы назначить соответствующую радиосеть ячейке предустановки.

    {"["}italic]УВЕДОМЛЕНИЕ COMSEC: Эта карточка является документом ограниченного доступа. Уничтожить перед угрозой захвата. Частоты действуют на всём театре военных действий; исходите из того, что противник располагает собственной копией.[/italic]

anprc-slot-max-reached = Достигнуто максимальное количество предустановок (4). Сначала удалите одну из них.

anprc-monitor-no-transmit = МОНИТОРИНГ АКТИВЕН — радиостанция работает только на приём. Отключите MON, чтобы передавать.

anprc-ct-mode-no-fill = РЕЖИМ CT — для передачи требуется криптографический ключ. Сначала загрузите ключ COMSEC.

anprc-scan-switched = СКАНИРОВАНИЕ — радиообмен на [{ $label }] (P{ $slot } · { $channel }). Активная радиосеть переключена.

anprc-squelch-suppressed = *шумоподавление*

anprc-crypto-not-equipped = Для загрузки криптографического ключа радиостанция должна быть надета.
anprc-crypto-already-loaded = Уже загружено: { $designation }. Сначала выполните обнуление.
anprc-crypto-loaded = { $designation } загружено. Передачи зашифрованы.
anprc-crypto-zeroized = { $designation } обнулено. Передачи не защищены.
anprc-crypto-destroyed = { $designation } физически уничтожено. Ключ невозможно восстановить.
anprc-crypto-no-card = Криптографический ключ не загружен.
anprc-crypto-wrong-faction = Это устройство загрузки ключей несовместимо с вашей радиостанцией.
anprc-crypto-examine-empty = Криптографический ключ не загружен. Передачи не защищены.
anprc-crypto-examine-loaded = { $designation } загружено ({ $faction }).
anprc-crypto-examine-stale = { $designation } загружено ({ $faction }) — УСТАРЕЛО, больше не защищает радиообмен.

anprc-comsec-unsecured = ПРЕДУПРЕЖДЕНИЕ COMSEC: Передача по { $channel } ({ $faction }) без криптографического ключа. Радиообмен доступен для чтения всем сторонам.

anprc-recrypto-no-card = Действующая карта ключа не загружена. Сначала вставьте карту ключа своей фракции.
anprc-recrypto-stale-card = Эта карта уже была признана устаревшей. Для смены ключей вставьте актуальную карту.
anprc-recrypto-foreign-card = СМЕНА КЛЮЧЕЙ ОТКЛОНЕНА — загруженный ключ не соответствует органу, выдавшему эту радиостанцию.
anprc-recrypto-ordered = СМЕНА КЛЮЧЕЙ COMSEC ПРИКАЗАНА: все карты ключей { $faction }, выданные до этого приказа, теперь считаются устаревшими. Запросите замену через обычный канал снабжения.
anprc-recrypto-not-authorized = СМЕНА КЛЮЧЕЙ ОТКЛОНЕНА — для смены ключей требуются командные полномочия COMSEC.
anprc-recrypto-button = ПРИКАЗАТЬ СМЕНУ КЛЮЧЕЙ — АННУЛИРОВАТЬ КЛЮЧИ ФРАКЦИИ
anprc-recrypto-button-confirm = НАЖМИТЕ ЕЩЁ РАЗ ДЛЯ ПОДТВЕРЖДЕНИЯ — АННУЛИРУЕТ ВСЕ КЛЮЧИ ФРАКЦИИ
anprc-recrypto-superseded-notice = СМЕНА КЛЮЧЕЙ COMSEC — загруженный вами ключ устарел. Запросите замену.

anprc-battery-depleted = Радиостанция разряжена. Вставьте батарею.
anprc-battery-empty = AN/PRC-117G выключается: батарея разряжена.
anprc-battery-insufficient = Недостаточно заряда батареи для передачи.

anprc-unknown-station = НЕИЗВЕСТНАЯ СТАНЦИЯ
anprc-radio-check-call = ВСЕМ СТАНЦИЯМ, Я { $station }, ПРОВЕРКА СВЯЗИ, ПРИЁМ.
anprc-radio-check-report = ОТВЕТЫ НА ПРОВЕРКУ СВЯЗИ — ПРИЁМ РАЗБОРЧИВЫЙ: { $clear } | ПРИЁМ С ПОМЕХАМИ: { $degraded }
anprc-radio-check-nothing-heard = ОТВЕТА НЕТ
anprc-radio-check-interference = ПОМЕХИ В РАДИОСЕТИ — азимут сильнейшего источника { $bearing }.

anprc-verb-plant = Развернуть ретранслятор
anprc-verb-packup = Свернуть радиостанцию
anprc-retrans-planted = Радиостанция закреплена на месте и начинает работать как автономный ретранслятор.
anprc-retrans-packed = Ретрансляционная станция свёрнута обратно в переносную радиостанцию.
anprc-retrans-pickup-blocked = Она закреплена как ретрансляционная станция. Сначала сверните её.

anprc-verb-handset = Взять трубку
anprc-verb-handset-release = Повесить трубку
anprc-handset-taken = Вы снимаете проводную трубку с { $radio }.
anprc-handset-released = Вы возвращаете трубку на { $radio }.
anprc-handset-in-use = Кто-то уже использует эту трубку.
anprc-handset-hands-full = Чтобы взять трубку, нужна свободная рука.
anprc-handset-cord = Провод вырывает трубку из вашей руки, когда вы отходите слишком далеко.
anprc-handset-radio-gone = Трубка отключается.
anprc-handset-hint = Пока вы держите трубку, обычная речь передаётся по активной радиосети станции. Говорите шёпотом, чтобы не выходить в эфир.

# Поисковый приёмник
anprc-sweep-started = Радиостанция покидает радиосеть и начинает сканировать диапазон. Пока вы не остановите поиск, вы ничего не услышите и не сможете передавать.
anprc-sweep-needs-online = Для поиска радиостанция должна быть включена и надета.
anprc-sweep-aborted = Поиск прекращается из-за отключения радиостанции.
anprc-sweep-aborted-power = Батарея разряжается, и поиск прекращается.
anprc-sweep-tx-blocked = Радиостанция сканирует диапазон. Остановите поиск перед передачей.
anprc-sweep-contact = Поиск сужается. Что-то передаёт на частоте { $freq } МГц.
anprc-sweep-resolved = ОБНАРУЖЕНО: { $freq } МГц — { $net }. Передачи носимых станций в этой сети теперь дают приблизительный пеленг, а каждое сообщение добавляет попытку подбора ключа на панели.
anprc-sweep-unknown-net = НЕОПОЗНАННАЯ РАДИОСЕТЬ

# Журнал радиосети на бумаге
anprc-log-print-empty = В журнале нет ничего, что стоило бы записывать.
anprc-log-printed = Вы переписываете на бумагу { $count } записей журнала.

anprc-log-frequency-unknown = ЧАСТОТА НЕИЗВ.

# Языки, которые невозможно передавать по радио
anprc-language-no-radio = { $language } невозможно передать по радиосети.

anprc-chat-intercept-line = [color={ $color }]\[INT\] "{ $message }"[/color]

anprc-fixed-net-bearing = ПЕЛЕНГ { $net }: направление на говорящего — { $bearing }, { $band ->
    [near] близко.
    [mid] на некотором расстоянии.
   *[far] далеко.
}
