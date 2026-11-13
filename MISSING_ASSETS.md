# MISSING ASSETS — недостающие спрайты первого уровня

Дата: 2026-11-13  
Область: только визуальные 2D-ресурсы для 18 комнат первого уровня.  
Пути к существующим ассетам указаны относительно корня проекта.

## 1. Что уже есть и не требует замены

### Окружение и фоны

| Ресурс | Путь | Можно использовать |
|---|---|---|
| Небо | `Assets/Resources/Level01/Sky.png` | все зоны, параллакс |
| Горы | `Assets/Resources/Level01/Mountains.png` | дальний план |
| Лес | `Assets/Resources/Level01/Forest.png` | зоны I и III |
| Передний план | `Assets/Resources/Level01/Foreground.png` | зоны I и III |
| Земля | `Assets/Resources/Level01/Ground.png` | наземные сегменты |
| Заливка грунта | `Assets/Resources/Level01/DirtFill.png` | низ длинных площадок |
| Остров/платформа | `Assets/Resources/Level01/Platform.png` | основа водных островов |
| Лагерь | `Assets/Resources/Level01/CampWorld.png` | комната 01 и части комнаты 14 |
| Озеро | `Assets/Resources/Level01/Lake.png` | фон зоны II |
| Ограда арены | `Assets/Resources/Level01/ArenaFence.png` | комнаты 13, 16, 17 |
| Финиш | `Assets/Resources/Level01/FinishGate.png` | комната 18 |
| Декорации | `Assets/Resources/Level01/Props.png` | 9 отдельных пропов после нарезки |

`Props.png` содержит: траву, цветы, грибы, папоротник, камень, пень, бревно, указатель и фонарь. Эти элементы не нужно рисовать заново — нужно нарезать лист на отдельные спрайты.

### Бой и VFX

| Ресурс | Путь |
|---|---|
| Hit spark | `Assets/Resources/HitSpark.prefab`, `Assets/Art/Combat/HitSpark.png` |
| Dizzy stars | `Assets/Art/VFX/DizzyStars/` |
| Fire trail | `Assets/Art/VFX/FireTrail/` |
| Revive bubble | `Assets/Art/VFX/ReviveBubble/` |
| Charge VFX | `Assets/VFX/Art/ChargeVFX/` |
| Girl lightning | `Assets/Resources/VFX/GirlLightning/` |

### Персонажи, враги и UI

| Ресурс | Путь |
|---|---|
| Хомяк | `Assets/Prefabs/Hamster.prefab`, `Assets/Art/Hamster/` |
| Хомячиха | `Assets/Prefabs/HamsterGirl.prefab`, `Assets/Art/HamsterGirl/` |
| Гиена | `Assets/Prefabs/Hyena.prefab`, `Assets/Art/Enemies/Hyena/` |
| Босс | `Assets/Art/Enemies/HyenaBoss/`, prefab-вариант в `Assets/Scenes/Level01_VerticalSlice.unity` |
| HUD здоровья | `Assets/Resources/UI/HealthPlates/` |
| Меню и пауза | `Assets/Art/UI/MainMenu/`, `Assets/Resources/UI/PauseMenu_Signboard.png` |

## 2. Основные недостающие спрайты

Формат размеров указан ориентировочно под PPU `100`; итоговый размер можно уменьшить без потери качества, если сохранить читаемость на камере `orthographicSize = 10…15`.

| № | Название | Комнаты | Для чего | Приоритет | Тип и формат | Размер/соотношение | Прозрачность | Анимация | Ориентир |
|---:|---|---|---|---|---|---|---|---|---|
| 1 | `GroundEdge_Set` | 02–18 | торцы земли и плавные края площадок | обязательный | Sprite Sheet / 4–6 спрайтов | 1024×512, отдельные элементы 128–256 px | да | нет | край `Ground.png`, `Platform.png` |
| 2 | `FloatingPlatform_Set` | 03–16 | малые, средние и длинные платформы с единым верхом | обязательный | Sprite Sheet / 3–5 вариантов | 2048×1024, элементы 4–12 юнитов | да | нет | `Platform.png` |
| 3 | `SteppingStone_Set` | 07–09 | камни/кочки для мелководья | обязательный | Sprite Sheet / 3–4 варианта | 1024×512, элементы 2–4 юнита | да | нет | камни из `Props.png`, острова из `Platform.png` |
| 4 | `WoodenDock_Set` | 06–09 | доски, сваи, край дока, верёвка | обязательный | Sprite Sheet / 4–6 элементов | 2048×1024 | да | нет | дерево из `ArenaFence.png`, `Props.png` |
| 5 | `BrokenBridge_Set` | 05 | цельные и сломанные секции старого моста | обязательный | Sprite Sheet / 6–8 элементов | 2048×1024; секции 4–6 юнитов | да | нет | доски и верёвки `ArenaFence.png` |
| 6 | `WaterSurface_Tile` | 06–10 | горизонтальная поверхность воды | обязательный | Tileable PNG / 2–4 кадра ряби | 512×256, бесшовно по X | да, верхняя кромка | да, 2–4 кадра | вода в `Lake.png` |
| 7 | `WaterShore_Edge` | 06–07 | линия перехода земли в воду | обязательный | PNG / 3 варианта | 512×256 | да | нет | берег `Lake.png` |
| 8 | `Waterfall_Column` | 10 | вертикальный поток воды | обязательный | Sprite Sheet / 4 кадра | 512×2048 или 4×512×1024 | да | да, loop 8–12 fps | вода `Lake.png` |
| 9 | `Waterfall_Splash` | 10 | брызги у основания и на уступах | обязательный | Sprite Sheet / 6–8 кадров | 512×256 | да | да, loop | `Lake.png`, `HitSpark.png` |
| 10 | `RockCliff_Set` | 10 | скалы и уступы водопада | обязательный | Sprite Sheet / 4–6 элементов | 2048×1024 | да | нет | камни `Props.png`, горы `Mountains.png` |
| 11 | `Trap_Spikes_Set` | 15 | шипы/ловушки на опасной тропе | обязательный | Sprite Sheet / 3 состояния | 1024×512, элемент 1–2 юнита | да | опционально 2–4 кадра | `HitSpark.png`, металл `ArenaFence.png` |
| 12 | `CrumblingPlatform_Set` | 15 | разрушаемая платформа | обязательный, если используется механика разрушения | Sprite Sheet / 3 состояния + обломки | 1024×512 | да | 3–4 кадра | `Platform.png`, `Ground.png` |
| 13 | `BreakableCrate` | 04, 14, 15 | разрушаемый ящик | дополнительный | PNG / 3 состояния + обломки | 256×256 | да | 3–4 кадра | ящики в `CampWorld.png` |
| 14 | `BreakableBarrel` | 14, 15 | разрушаемая бочка | дополнительный | PNG / 3 состояния + обломки | 256×256 | да | 3–4 кадра | бочки в `CampWorld.png` |
| 15 | `Checkpoint_Campfire` | 01, 06, 11, 16 | визуальный маркер чекпоинта | обязательный | PNG / 2 состояния + 6 кадров огня | 512×512 | да | да, loop | костёр `CampWorld.png`, `Props.png` |
| 16 | `Reward_Acorn` | 14, 18 | награда/коллекционный предмет | обязательный, если награда визуальная | Sprite Sheet / 6–8 кадров | 256×256 | да | да, loop | стиль `Props.png` |
| 17 | `BossHealth_Frame` | 17 | отдельная полоса здоровья босса | обязательный, если босс-HUD делается спрайтовым | UI PNG, 9-slice желателен | 1024×128 | да | нет | `HealthPlates/Hamster_HealthPlate.png` |
| 18 | `BossHealth_Fill` | 17 | заполнение полосы босса | обязательный вместе с рамкой | UI PNG, tileable | 512×64 | да | нет | `HealthPlates/Hamster_HealthFill.png` |
| 19 | `RoomTitle_Ribbon` | 02–18 | название комнаты при входе | дополнительный | UI PNG | 1024×256 | да | появление/исчезновение кодом | `MainMenu_Signboard.png` |
| 20 | `InteractPrompt` | 04, 14, 18 | подсказка E/кнопки у объекта | дополнительный | UI PNG или sprite sheet | 256×256 | да | 2 кадра пульсации | `HealthPlates` |

## 3. Дополнительные недостающие спрайты

Эти ресурсы не блокируют прохождение, но заметно улучшат читаемость и разнообразие.

| № | Название | Комнаты | Назначение | Тип/формат | Размер | Прозрачность | Анимация | Ориентир |
|---:|---|---|---|---|---|---|---|---|
| 21 | `ForestTree_Large_01/02` | 03, 11–16 | крупные деревья на переднем плане | PNG / 2 варианта | 1024×1536 | да | нет | `Forest.png`, `Foreground.png` |
| 22 | `ForestBush_Foreground` | 03, 11–16 | кусты перед игроком | PNG / 2–3 варианта | 1024×512 | да | лёгкое покачивание кодом | `Foreground.png`, `Props.png` |
| 23 | `LilyPad_Set` | 07–09 | украшение воды | PNG / 3 варианта | 256×128 | да | покачивание кодом | `Lake.png` |
| 24 | `Reeds_Cattails_Set` | 06–10 | камыши у воды | PNG / 2–3 варианта | 256×512 | да | нет | `Lake.png` |
| 25 | `UnderwaterFloor` | 07–09 | дно под прозрачной водой | Tileable PNG | 512×256 | да | нет | `Lake.png` |
| 26 | `WaterSplash_Hit` | 07–10 | всплеск при падении/прыжке | Sprite Sheet / 6 кадров | 256×256 | да | да | `HitSpark.png` |
| 27 | `LandingDust` | 03–16 | пыль при приземлении | Sprite Sheet / 4–6 кадров | 256×256 | да | да | `HitSpark.png` |
| 28 | `TrapDebris` | 15 | обломки ловушки/платформы | Sprite Sheet / 4–6 кадров | 256×256 | да | да | `HitSpark.png` |
| 29 | `BossSlam_Telegraph` | 17 | читаемая зона удара босса | PNG / 3–4 кадра | 512×256 | да | да | `HitSpark.png` |
| 30 | `Friend_NPC_Alina` | 14 | игровой NPC из концепта | Sprite Sheet / idle 4–6 кадров | 1024×1024 | да | да | `Assets/Art/Friends/Concepts/Alina_Tourist.png` |
| 31 | `Friend_NPC_Artem` | 14 | игровой NPC из концепта | Sprite Sheet / idle 4–6 кадров | 1024×1024 | да | да | `Assets/Art/Friends/Concepts/Artem_GrillMaster.png` |
| 32 | `Friend_NPC_Ksusha` | 14 | игровой NPC из концепта | Sprite Sheet / idle 4–6 кадров | 1024×1024 | да | да | `Assets/Art/Friends/Concepts/Ksusha_Trailblazer.png` |
| 33 | `Friend_NPC_Lyokha` | 14 | игровой NPC из концепта | Sprite Sheet / idle 4–6 кадров | 1024×1024 | да | да | `Assets/Art/Friends/Concepts/Lyokha_Cat.png` |
| 34 | `Friend_NPC_Sanya` | 14 | игровой NPC из концепта | Sprite Sheet / idle 4–6 кадров | 1024×1024 | да | да | `Assets/Art/Friends/Concepts/Sanya_FestiveGlade.png` |
| 35 | `Friend_NPC_Yana` | 14 | игровой NPC из концепта | Sprite Sheet / idle 4–6 кадров | 1024×1024 | да | да | `Assets/Art/Friends/Concepts/Yana_Gifts.png` |
| 36 | `Hyena_Elite_Tint` | 15 | визуальное отличие сильной гиены | PNG-маска или отдельный sheet | 320×240 на кадр | да | можно использовать tint материала | `Assets/Art/Enemies/Hyena/` |

## 4. Что можно не рисовать

- Небо, горы, лесной фон и передний план — уже есть.
- Землю, траву, камни, цветы, папоротник, пни и брёвна — можно взять из `Ground.png`, `Platform.png` и `Props.png`.
- Лагерные палатки, костёр, столы, бочки и ящики — уже есть внутри `CampWorld.png`, если не требуется отдельная интерактивность.
- Обычную гиену, босса, героев, их атаки, урон, смерть, оживление и суперы — не перерисовывать.
- Костёр чекпоинта можно собрать из костра в `CampWorld.png`, но отдельный чистый спрайт предпочтительнее.
- Указатель и фонарь уже есть в `Props.png`.
- Арену и финишные ворота не нужно рисовать заново: есть `ArenaFence.png` и `FinishGate.png`.

## 5. Требования к импорту новых ресурсов

- PPU: `100`, как у текущего окружения.
- Pivot: `0.5, 0.5` для декораций; для платформ допустим pivot по верхнему центру, если prefab сразу учитывает это.
- Filter Mode: `Bilinear` для живописного стиля, `Point` только для пиксельных элементов.
- Compression: без сильной потери альфа-краёв; для крупных фонов проверить размер в сборке.
- Все одиночные объекты — с прозрачным фоном.
- Sprite Sheet должен иметь прозрачные промежутки между кадрами и равный размер кадров.
- Анимации воды, огня и брызг — loop-friendly, первый и последний кадры не должны давать скачок.

## 6. Приоритет закупки/отрисовки

1. `FloatingPlatform_Set`, `GroundEdge_Set`, `SteppingStone_Set`, `WaterSurface_Tile`, `WaterShore_Edge` — без них не собрать зону II.
2. `BrokenBridge_Set`, `Waterfall_Column`, `Waterfall_Splash`, `RockCliff_Set` — ключевые комнаты 05 и 10.
3. `Checkpoint_Campfire`, `Trap_Spikes_Set`, `BossHealth_Frame/Fill` — игровой цикл и читаемость.
4. `CrumblingPlatform_Set`, `Reward_Acorn`, `BreakableCrate/Barrel` — интерактив и награды.
5. NPC, лесные деревья, вода-декор и дополнительные VFX — после проверки базового прохождения.

## 7. Итог

Нарисовать заново нужно прежде всего **модульное окружение**: края земли, платформы, мост, воду, водопад, камни, ловушки и чекпоинт. Персонажи, враги, босс, HUD и основные VFX уже покрывают требования первого уровня. Концепты друзей из `Assets/Art/Friends/Concepts/` не являются игровыми спрайтами и требуют отдельной анимационной подготовки только если комната 14 будет включать их как NPC.

