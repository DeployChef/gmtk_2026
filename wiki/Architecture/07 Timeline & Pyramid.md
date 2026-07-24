# 07 Timeline & Pyramid

← [[06 Inventory]] | [[Index]]

## Роль

1. **`ITimelineService`** — clock партии, фазы из SO, прогресс оффера, смена фазы, гнев при провале, debug jump.
2. **`IPyramidTimerService`** — countdown; baseline на старте; `+secondsReward` за верный оффер; `wrongOfferingTimerDelta` (±) при reject; `00:00` → lose.

Пирамида — drop-zone. Кара фазы → шина → `IDisasterManager.TryStrikeRandomHouse()` (только **Built** слоты; руины / стройка вне пула). См. [[08 Buildings]].

## Баланс (без автоматематики)

Автоформула «сумма наград = duration → снова 99» **не используется**.  
Таймер к концу фазы **плавает** — стресс настраивается вручную:

- у каждой позиции оффера свой `secondsReward`;
- глобально (или per-phase) `wrongOfferingTimerDelta` (± число при неверном дропе).

`baselineSeconds` — только старт рана / debug reset.

## Данные (ScriptableObject)

```text
GameTimelineConfig (SO)
  baselineSeconds                 // старт, напр. 99
  wrongOfferingTimerDelta         // ± при reject (напр. -1)
  yearsPerRealtimeSecond
  phases[]: PhaseDefinition
    durationSeconds
    color / title / tooltip
    requirements[]: PhaseOfferItem
      resource / count / secondsReward
    startingCards[]: { ResourceDefinition, count }      // StartRun + Cheat Panel jump
    startingBuildings[]: { buildingId, active, workers } // пусто = не трогать здания
    unlockBuildingIds[]           // на PhaseStarted: Locked → Buildable
    // modifiers[] — вторым заходом

CheatPanelConfig (SO) — отдельно; окно They Will Descend → Cheat Panel
  grantAllCardsOnJump             // после Jump дополнительно заполнить каталог
  allCardsCatalog[]
  grantAllCardsCount / unlimitedGrantCount
```

Loadout (`startingCards` / `startingBuildings`) применяется только при `StartRun` (фаза 0) и Cheat Panel **Jump**. Обычный advance фазы — нет.

`CheatPanelConfig` **не** вешается на `GameLifetimeScope` — панель сама берёт SO и передаёт в `GrantAllCardsFromCatalog(cheats)`.

Editor: **They Will Descend → Cheat Panel** — Grant All Cards + Jump to phase (Play Mode).

`unlockBuildingIds` применяется на **каждом** `PhaseStarted` (в т.ч. обычный advance): слоты переходят Locked → Buildable. Cost/duration — из `BuildingDefinition`, не из фазы. См. [[08 Buildings]].
## Подношение

```
DnD ресурс → Pyramid
  → если не нужен текущему офферу:
        reject
        AddSeconds(wrongOfferingTimerDelta)   // может быть -1, 0, …
  → иначе:
        inventory.TryRemove
        progress++
        AddSeconds(+item.secondsReward)
        OfferingSubmittedEvent
```

## Сервисы / события

| Сервис | Публикует |
| --- | --- |
| `ITimelineService` | `PhaseStarted`, `PhaseCompleted`, `PhaseFailed`, years tick |
| `IPyramidTimerService` | `PyramidTimerChanged`, `PyramidTimerExpired` |

### Debug phase jump

Окно **They Will Descend → Cheat Panel** (Play Mode) → Jump / Grant All. Настройки карт — `CheatPanelConfig`. Не в игровом UI.

## UI

| Элемент | Слой |
| --- | --- |
| Сегменты фаз + годы | Root TopBar |
| Таймер над пирамидой | World Space |
| Placeholder пирамиды | Game |

## MVP vs позже

| MVP (код) | Вторым заходом |
| --- | --- |
| Фазы + офферы с `secondsReward` | Модификаторы производства эры |
| Reject + `wrongOfferingTimerDelta` | Прочие катаклизмы / lose VFX |
| Гнев-молния, TopBar сегменты, World-таймер | Спрайт пирамиды |
| Cheat Panel Jump + phase loadout | Win state rewind |

## Реализованные типы

| Слой | Типы |
| --- | --- |
| Core | `GameTimelineConfig`, `PhaseDefinition`, `PhaseOfferItem`, `ITimelineService`, `IPyramidTimerService`, timeline/pyramid events |
| Gameplay | `TimelineService`, `PyramidTimerService`, `TimelineSessionDriver`, `PyramidOfferingPoint`, `PhaseLoadoutApplier` |
| UI | `TimelineHudView`, `TimelinePhaseSegmentView`, `PyramidTimerWorldHud`, `PyramidCardDropZone`, `PyramidOfferWorldHud` |
| Editor | `CheatPanelWindow` — Jump / Grant All; `CheatPanelConfig` |
| Main | регистрация в `GameLifetimeScope`; `GameStartState` → `StartRun()` |

## Связь

- GDD: [[../GDD/04 Timeline & Events]] · [[../GDD/05 Win Lose]]
- Шина: [[05 Event Bus]]
- Кары: `IDisasterManager` / `IThunderService`
