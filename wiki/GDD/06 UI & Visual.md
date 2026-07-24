# 06 UI & Visual

← [[05 Win Lose]] | [[Home]] | Далее → [[07 Narrative & Onboarding]]

## 8. Визуал и UI

### Камера

- 2D, вид сбоку
- Плавный лерп-зум на Пирамиду при выдаче новых требований или начале катаклизма

### Стиль

Темный пиксель-арт (или стилизованное 2D). Мрачная атмосфера надвигающейся угрозы.

### Слои UI

| Слой | Где | Что |
| --- | --- | --- |
| **HUD здания** | World Space на слоте/здании | зависит от состояния слота (см. ниже) |
| **Оффер Пирамиды** | World Space у Пирамиды | позиции оффера фазы |
| **GameHud Overlay** | Screen Space Overlay на Root | верхняя и нижняя рамка поверх всего геймплея |

### GameHud Overlay (рамка)

```
GameHud (Screen Space Overlay)
├── TopBar
│   ├── Timeline          ← N сегментов-слайдеров фаз в ряд (+ тултипы)
│   └── YearsLabel        ← сколько «лет» прошло
└── BottomBar
    └── CardTrays (×8)    ← лотки по типу ResourceDefinition (стек)
```

Отдельного **BuildMenu / сайдбара строительства нет** — стройка только через World HUD на слоте (см. [[03 Economy#5.2. Строительство (оффер на слоте)|03 Economy]]).

**Таймер Конца Света** — не в Overlay, а **World Space над Пирамидой** (см. [[04 Timeline & Events]]).

- **8 лотков** — по одному на тип; карты **стакаются** со смещением, тянуть можно одну.
- Обычные лотки ограничены `TrayCapacity`; переполнение → burn.
- Лоток **Villager**: Available / Assigned / Total, без hard-cap.
- Старт рана: **1×** `Resource_Villager` в лоток жителей.
- Позже: fly-in от здания в лоток (spawn point дома = точка вылета).
- DnD: из лотка на drop-zone здания (ресурс → build-cost / production input; житель → +1 worker на **Built**) или на Пирамиду (**только ресурсы**, в т.ч. Кровь; житель на Пирамиду — нет).

Подробности логики: [[../Architecture/06 Inventory|Architecture: Inventory]].

### HUD здания (World Space, на самом слоте)

Зависит от состояния ([[03 Economy#5.2. Строительство (оффер на слоте)|Economy]]):

| Состояние | HUD |
| --- | --- |
| **Locked** | нет (только руины) |
| **Buildable** | `BuildingConstructionHud`: иконки cost + `stored/required` |
| **Constructing** | тот же HUD: прогресс-бар таймера стройки |
| **Built** | `BuildingWorldHud` — production (ниже) |

Два разных World HUD на слоте (не один общий). Construction — без workers и без output.

#### Built — production HUD

- Иконки требуемых ресурсов (из рецепта) + `stored/required`
- Прогресс-бар создания
- Счётчик рабочих: `[-] 2 [+]`
  - **`[+]`** — только если в лотке есть свободный житель (иначе disabled)
  - **`[-]`** — возвращает карточку жителя в лоток
  - DnD карты жителя на drop-zone = тот же `+1`
- Drop-zone: принимает ресурсные карты и карты жителей

---

Связанные: [[02 Entities]] · [[03 Economy]] · [[04 Timeline & Events]] · [[07 Narrative & Onboarding]]
