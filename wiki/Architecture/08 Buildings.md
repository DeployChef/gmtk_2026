# 08 Buildings

← [[07 Timeline & Pyramid]] | [[Index]]

## Роль

Слоты зданий на сцене `Game`: производство и **стройка через оффер** (без меню).

GDD: [[../GDD/03 Economy#5.2. Строительство (оффер на слоте)|03 Economy]].

## Состояния слота

`Locked` → `Buildable` → `Constructing` → `Built`

| Переход | Триггер |
| --- | --- |
| → Buildable | `PhaseStarted` + `buildingId` в `phase.unlockBuildingIds` |
| → Constructing | все позиции `buildCost` закрыты |
| → Built | истёк `buildDurationSeconds` |

Стартовые Built задаются loadout (`startingBuildings`) / phase 0 — не через unlock.

## Данные

```text
BuildingDefinition (SO)
  buildingId / displayName
  buildCost[]: { ResourceDefinition, count }
  buildDurationSeconds
  // production (MVP: поля как у BuildingRecipe, или ref на recipe)
  inputResources[] / inputAmounts[]
  outputResource
  productionDurationSeconds
  workersRequired
```

Сейчас в коде: `BuildingDefinition` (бывший `BuildingRecipe`) — build cost + production на одном SO.

`PhaseDefinition.unlockBuildingIds[]` — только список id; cost не дублировать в фазе.

## Поток стройки

```
PhaseStarted(unlock ids)
  → slot Locked → Buildable, показать BuildOffer HUD

DnD resource → Buildable slot
  → если resource в buildCost и ещё нужен: accept, progress++
  → иначе reject (без штрафа таймера Пирамиды)
  → если все позиции закрыты: → Constructing, старт таймера

Constructing tick
  → progress bar (как produce HUD)
  → done: → Built, hide build HUD, enable production HUD
  → BuildingConstructedEvent
```

## Молния / кары

`IDisasterManager.TryStrikeRandomHouse` выбирает только слоты в **`Built`**.  
Locked / Buildable / Constructing — вне пула целей.

## UI

| Состояние | View |
| --- | --- |
| Locked | руины, без HUD |
| Buildable | `BuildingConstructionHud` — cost icons + counts |
| Constructing | тот же — progress slider |
| Built | `BuildingWorldHud` — production |

## События (черновик)

| Событие | Когда |
| --- | --- |
| `BuildingUnlockedEvent` | Locked → Buildable |
| `BuildingBuildProgressEvent` | принят ресурс в cost / изменился stored |
| `BuildingConstructionStartedEvent` | → Constructing |
| `BuildingConstructedEvent` | → Built |

## MVP vs позже

| MVP | Позже |
| --- | --- |
| Состояния + cost + timer | Апгрейды |
| Unlock по фазе | VFX руин → дома |
| Молния только Built | Отдельный спрайт «строящегося» |

## Связь

- GDD: [[../GDD/03 Economy]] · [[../GDD/06 UI & Visual]]
- Фазы: [[07 Timeline & Pyramid]]
- Шина: [[05 Event Bus]]
- Кары: `IDisasterManager`
