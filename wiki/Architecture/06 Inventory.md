# 06 Inventory (Hand / Trays)

← [[Index]] | [[../GDD/02 Entities|GDD Entities]] · [[../GDD/06 UI & Visual|GDD UI]]

## Решение

Логика — **`IInventory` / `InventoryService`**. UI только слушает события.

Лотки на сцене **ставит человек** (`CardTrayView` × N). Код их **не создаёт**.

## Сервис

| API | Смысл |
| --- | --- |
| `TryAdd(ResourceDefinition)` | +1 available; при cap — отказ (burn UI позже) |
| `TryRemove` | −1 available |
| `GetCount` / `GetDefinition` | запросы |
| `Clear` | старт → `InventoryClearedEvent` |

В инвентаре только **available**. Assigned жители — workers зданий.

## UI

| Компонент | Роль |
| --- | --- |
| `InventoryTraysView` | список `CardTrayView[]`, префаб карты, подписки на шину |
| `CardTrayView` | один тип: `Resource`, `Stack Root`, `Counter` |

Счётчик: ресурсы `count/capacity`, жители `available/total`.

## Старт

Стартовые карты / здания задаются в **`GameTimelineConfig` → Phase 0 → Starting Cards / Starting Buildings**.  
`GameStartState` только вызывает `ITimelineService.StartRun()` (+ BGM). Hardcode «1 villager» снят.

Debug jump на фазу N (кнопки в Inspector у SO, только Play Mode) тоже применяет loadout этой фазы.  
Обычный переход фазы mid-run loadout **не** трогает.
