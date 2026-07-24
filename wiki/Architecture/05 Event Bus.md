# 05 Event Bus

← [[04 Game Director]] | [[Index]]

## Зачем

Связь **логика → отображение** и между системами идёт через **шину**. Сервисы и MonoBehaviour **не вызывают** HUD/VFX напрямую.

| Без шины | С шиной |
| --- | --- |
| `hud.SetTimer(t)` из пирамиды | `bus.Publish(PyramidTimerChangedEvent)` |
| здание знает про аудио и диалоги | здание публикует `ResourceProducedEvent` |
| жёсткие ссылки между системами | подписки по типу сообщения |

## Контракт

```csharp
public interface IGameEventBus
{
    void Publish<T>(T message) where T : struct;
    IDisposable Subscribe<T>(Action<T> handler);
    IDisposable Subscribe<T>(Action<T> handler, Predicate<T> filter);
}
```

- Сообщения — **immutable** (`readonly struct`), без ссылок на `MonoBehaviour`
- Один стиль на проект: **только struct** (как выше)
- Подписка возвращает `IDisposable` — отписка в `OnDestroy` / при Dispose scope

## Где регистрируется

На старте оснастки — **singleton в Root** (рядом с `GameDirector` и `IAudioManager`), чтобы все слои видели одну шину.

Когда появятся child-scopes, шина остаётся на родителе: дети резолвят тот же `IGameEventBus`, без «bridge» и прямых вызовов вверх/вниз по иерархии scope.

```csharp
builder.Register<IGameEventBus, GameEventBus>(Lifetime.Singleton);
```

## Поток данных (игровой)

```
Ввод / DnD / UI здания
  → геймплей-система (назначение NPC, крафт, подношение)
  → bus.Publish(...)
  → подписчики: HUD, Audio, VFX, GameDirector, диалоги Шамана, таймлайн
```

## Черновые события под GDD

Не полный список — ориентир для первой реализации:

| Событие | Когда | Кто слушает (примерно) |
| --- | --- | --- |
| `NpcAssignedEvent` / `NpcUnassignedEvent` | DnD карты жителя на здание / `[-]` вернул карту в rail | HUD здания, CardsRail |
| `ResourceProducedEvent` | здание выдало карточку (ресурс / житель / актибка) | спавн в CardsRail |
| `CardDroppedOnBuildingEvent` | DnD ресурса/активки/жителя | крафт / бафф / +worker |
| `OfferingSubmittedEvent` | ресурс на Пирамиду (в т.ч. Кровь) | прогресс оффера фазы, +N сек таймера, аудио, Шаман |
| `PhaseStartedEvent` / `PhaseCompletedEvent` | смена / закрытие эры | UI таймлайна, модификаторы производства, Шаман |
| `PhaseFailedEvent` / `GodsDemandFailedEvent` | оффер фазы не закрыт к дедлайну | `IDisasterManager` (молния), UI, аудио |
| `TimelineYearsChangedEvent` | тик «лет прошло» | TopBar label |
| `PyramidTimerChangedEvent` | тик / порог тревоги | World HUD пирамиды, аудио |
| `PyramidTimerExpiredEvent` | remaining ≤ 0 | win/lose flow, VFX |
| `InventoryChangedEvent` | изменился available count типа | `InventoryTraysView` |
| `InventoryClearedEvent` | старт / clear | UI clear trays |

Имена и поля уточним при коде; принцип — данные для реакции, не «сделай сам UI».

## Пример формы сообщения

```csharp
public readonly struct ResourceProducedEvent
{
    public readonly int BuildingId;
    public readonly string ResourceId;
    public readonly bool BonusActiveDropped;

    public ResourceProducedEvent(int buildingId, string resourceId, bool bonusActiveDropped)
    {
        BuildingId = buildingId;
        ResourceId = resourceId;
        BonusActiveDropped = bonusActiveDropped;
    }
}
```

## Правила

1. Геймплей **публикует факты** («произвели дерево»), UI **рисует следствие**
2. Не тащить в событие целые view/сервисы — только id и примитивы
3. Не использовать шину вместо прямого синхронного запроса внутри одного модуля (локальный вызов метода ок)
4. Аудио: либо подписка на шину, либо точечный `IAudioManager.Play` из системы — оба варианта допустимы на старте; со временем предпочтительнее шина для «атмосферных» реакций

## Связь

- Оркестрация сцен / старт: [[04 Game Director]]
- Таймлайн и катаклизмы: [[../GDD/04 Timeline & Events]]
- Звук: [[03 Audio Manager]]
