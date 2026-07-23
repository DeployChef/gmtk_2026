# 04 Game Director

← [[03 Audio Manager]] | [[Index]] | Далее → [[05 Event Bus]]

## Роль

`GameDirector` — центральный оркестратор приложения: cold start, старт/рестарт сессии, переходы win/lose. Не содержит геймплейной логики зданий и карточек — только жизненный цикл.

Регистрируется в **Root** DI как singleton. Вызывается из `Startup` после `rootScope.Build()`.

## Интерфейс (черновик)

```csharp
public interface IGameDirector
{
    UniTask InitializeGameAsync();
    UniTask StartNewRunAsync();
    UniTask RestartRunAsync();
    void NotifyRunWon();
    void NotifyRunLost();
}
```

Имена методов зафиксируем при реализации; смысл такой:

| Метод | Назначение |
| --- | --- |
| `InitializeGameAsync` | Cold start: поднять сервисы, при необходимости загрузить `Game` additive, войти в стартовое состояние (онбординг / поселение) |
| `StartNewRunAsync` | Новая партия с чистого состояния |
| `RestartRunAsync` | Рестарт после поражения / из паузы — без лишней выгрузки Root |
| `NotifyRunWon` / `NotifyRunLost` | Реакция на [[../GDD/05 Win Lose\|Win/Lose]] (UI, звук, остановка таймлайна) |

## Cold start

```
Startup
  → RootLifetimeScope.Build()
  → IGameDirector.InitializeGameAsync()
       → (опц.) loading screen
       → подготовить шину / аудио
       → загрузить Game additive (когда появится) или активировать контент на Root
       → стартовое состояние: онбординг Шамана [[../GDD/07 Narrative & Onboarding|Onboarding]]
       → скрыть loading
```

## Сессия (run)

Один «забег» = от старта поселения до Win или Lose ([[../GDD/05 Win Lose|Win/Lose]]):

- идёт [[../GDD/04 Timeline & Events|таймлайн]] и главный таймер пирамиды
- директор **не** тикает экономику сам — системы публикуют события в [[05 Event Bus|шину]]
- директор слушает (или получает явный вызов) финальные состояния и переключает UI / музыку / возможность рестарта

## Что директор не делает

- не назначает NPC и не двигает карточки
- не считает крафт и не хранит инвентарь зданий
- не рисует HUD (это UI + подписки на шину)

## Регистрация

```csharp
// RootLifetimeScope / RootScopeExtensions
builder.Register<GameDirector>(Lifetime.Singleton).As<IGameDirector>();
```

Зависимости директора (через конструктор / inject): `IGameEventBus`, при необходимости `IAudioManager`, позже — контроллер сцен / child scope.

## Связь с другими доками

- Bootstrap: [[02 Scenes & Root LifetimeScope]]
- Сообщения: [[05 Event Bus]]
- Аудио при win/lose / напряжении: [[03 Audio Manager]]
