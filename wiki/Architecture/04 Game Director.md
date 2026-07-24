# 04 Game Director

← [[03 Audio Manager]] | [[Index]] | Далее → [[05 Event Bus]]

## Роль

`GameDirector` — верхний оркестратор: **Start** / **Restart**. Грузит/выгружает `Game`, затем `GameStartState.Enter()`. Не трогает карты и здания сам.

## Интерфейс

```csharp
public interface IGameDirector
{
    UniTask StartAsync();
    UniTask RestartAsync();
}
```

| Метод | Действие |
| --- | --- |
| `StartAsync` | Load `Game` additive → Build scope → `GameStartState.Enter()` |
| `RestartAsync` | Unload → Load → `GameStartState.Enter()` |

## GameStartState

Обычный класс с `Enter()` (без FSM):

- `ITimelineService.StartRun()` — фаза 0 + loadout из SO
- BGM

Регистрируется в `GameLifetimeScope`. Директор только резолвит и вызывает `Enter()`.

Стартовые карты/здания — в `GameTimelineConfig` phase 0, не в коде. См. [[07 Timeline & Pyramid]], [[06 Inventory]].

## Cold start

```
Startup
  → RootLifetimeScope.Build()
  → IGameDirector.StartAsync()
       → load Game + Build
       → GameStartState.Enter()
```

## Что директор не делает

- не спавнит карты напрямую
- не считает крафт / win-lose
- не рисует HUD

## Регистрация

```csharp
builder.Register<GameDirector>(Lifetime.Singleton).As<IGameDirector>();
```

## Связь

- Bootstrap: [[02 Scenes & Root LifetimeScope]]
- Шина: [[05 Event Bus]]
