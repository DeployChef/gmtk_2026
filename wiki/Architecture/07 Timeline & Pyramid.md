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
- глобально (или per-phase) `wrongOfferingTimerDelta` (± число при неверном дропе);
- **`productionModifiers[]`** на фазе — % скорости производства (эра).

`baselineSeconds` — только старт рана / debug reset.

### Era production modifiers

На `PhaseDefinition`:

| Поле | Смысл |
| --- | --- |
| `target` | `AllOutputs` / `Resource` / `BuildingId` |
| `resource` | при `Resource` — какой output замедлить/ускорить |
| `buildingId` | при `BuildingId` |
| `speedPercent` | −20 = на 20% медленнее; +10 = быстрее |
| `displayTitle` / `description` / `icon` | UI чип + тултип |

Формула: `EffectiveProdSec = Base / (1 + speedPercent/100)`  
В тике: `_progress += dt * SpeedMultiplier` (множители стакаются, если несколько матчей).

UI: `EraModifierBadgeView` как дом/пирамида — `iconsContainer` + `iconPrefab`, tint самой иконки зелёный/красный, tooltip на hover. `TimelinePhaseSegmentView` только Setup/Reveal.

См. [[../Balance/Balance|Balance]].

## Данные (ScriptableObject)

```text
GameTimelineConfig (SO)
  baselineSeconds / wrongOfferingTimerDelta / yearsPerRealtimeSecond
  runStartCards[] / runStartBuildings[]   // только StartRun
  phases[]: PhaseDefinition
    durationSeconds / color / title / tooltip
    requirements[]: PhaseOfferItem
    unlockBuildingIds[]                   // Locked → Buildable на PhaseStarted
    productionModifiers[]                 // era speed % (All / Resource / BuildingId)

CheatPanelConfig (SO) — They Will Descend → Cheat Panel
  grantAllCardsOnJump / allCardsCatalog / counts
  phaseLoadouts[]                         // index = phase index
    startingCards[]                       // карты на Jump (если grantAll выкл)
    builtBuildings[]                      // Built + workers; остальные Locked
```

`StartRun`: `runStartCards` / `runStartBuildings` + unlocks фазы 0.  
Cheat Panel **Jump**: сброс зданий по `builtBuildings` → cumulative `unlockBuildingIds` (0..phase) → карты (catalog или startingCards фазы чит-конфига).  
Обычный advance: только `unlockBuildingIds` текущей фазы (без сброса Built). См. [[08 Buildings]].
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
| Чипы era modifiers (иконка + тултип) | На сегменте таймлайна, над fill |
| Таймер над пирамидой | World Space |
| Placeholder пирамиды | Game |

## MVP vs позже

| MVP (код) | Вторым заходом |
| --- | --- |
| Фазы + офферы с `secondsReward` | Прочие катаклизмы / lose VFX |
| Reject + `wrongOfferingTimerDelta` | Спрайт пирамиды |
| Гнев-молния, TopBar сегменты, World-таймер | Win state rewind |
| Cheat Panel Jump + phase loadout | |
| Era `productionModifiers` + TopBar chips | |

## Реализованные типы

| Слой | Типы |
| --- | --- |
| Core | `GameTimelineConfig`, `PhaseDefinition`, `PhaseOfferItem`, `PhaseProductionModifier`, `ITimelineService`, `IPyramidTimerService`, timeline/pyramid events |
| Gameplay | `TimelineService`, `PyramidTimerService`, `TimelineSessionDriver`, `PyramidOfferingPoint`, `PhaseLoadoutApplier`; `ProductionBuilding` applies era speed mul |
| UI | `TimelineHudView`, `TimelinePhaseSegmentView`, `EraModifierBadgeView` (HLG strip), `PyramidTimerWorldHud`, `PyramidCardDropZone`, `PyramidOfferWorldHud` |
| Editor | `CheatPanelWindow` — Jump / Grant All; `CheatPanelConfig` |
| Main | регистрация в `GameLifetimeScope`; `GameStartState` → `StartRun()` |

## Связь

- GDD: [[../GDD/04 Timeline & Events]] · [[../GDD/05 Win Lose]]
- Шина: [[05 Event Bus]]
- Кары: `IDisasterManager` / `IThunderService`
