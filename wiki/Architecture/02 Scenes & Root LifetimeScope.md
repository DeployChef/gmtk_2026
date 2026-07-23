# 02 Scenes & Root LifetimeScope

← [[01 Folder Structure]] | [[Index]] | Далее → [[03 Audio Manager]]

## Принцип

**Root-сцена живёт вечно** (остаётся загруженной). Глобальные сервисы висят на ней. Отдельный геймплей при необходимости грузится **additive**, не через `DontDestroyOnLoad`.

```
┌──────────────────────────────────────────────┐
│  Root.unity  (в Build Settings, не выгружаем)│
│  ├─ Startup                                  │
│  ├─ RootLifetimeScope  ← родительский scope │
│  ├─ AudioManager                             │
│  ├─ EventSystem / корневой UI (позже)        │
│  └─ (опционально) loading screen             │
├──────────────────────────────────────────────┤
│  Game.unity  (additive, позже)               │
│  └─ поселение, слоты зданий, пирамида, HUD   │
│     + child LifetimeScope (когда понадобится)│
└──────────────────────────────────────────────┘
```

## Build Settings (старт джема)

| # | Сцена | Режим |
| --- | --- | --- |
| 0 | `Root.unity` | Единственная сцена в билде |
| — | `Game.unity` | Позже: `LoadSceneMode.Additive` из кода |

На раннем прототипе геймплей может временно жить **на Root**; additive `Game` добавляем, когда контент разъедется.

## Родительский LifetimeScope

На Root:

- `Startup` + `RootLifetimeScope` (VContainer)
- **Auto Run = выключен** — `Build()` вызывает `Startup`
- `AudioManager` (+ каталог, mixer) — см. [[03 Audio Manager]]

### Поток старта

```
Startup.Awake
  → guard (_started)
  → rootScope.Build()
  → Resolve IGameDirector → Initialize…
```

Псевдокод:

```csharp
// Startup
rootScope.Build();
var director = rootScope.Container.Resolve<IGameDirector>();
await director.InitializeGameAsync();
```

```csharp
// RootLifetimeScope.Configure
builder.Register<GameDirector>(Lifetime.Singleton).As<IGameDirector>();
builder.Register<IGameEventBus, GameEventBus>(Lifetime.Singleton);
builder.RegisterComponentInHierarchy<AudioManager>().As<IAudioManager>();
```

### Регистрация

Пока один Root-scope — вся регистрация прямо в `RootLifetimeScope.Configure`. Extensions имеет смысл, только когда появятся отдельные child-scopes и список разрастётся.

Минимум на старте в Root:

| Сервис | Как |
| --- | --- |
| `GameDirector` as `IGameDirector` | `Register(...).As<IGameDirector>()` |
| `AudioManager` as `IAudioManager` | `RegisterComponentInHierarchy<AudioManager>()` |
| `IGameEventBus` | singleton в Root |

Child-scopes (`App` / `Game`) — **позже**. Родитель всегда Root: дети видят зарегистрированные сервисы автоматически.

### План на потом: GameLifetimeScope на сцене

Когда появится additive `Game.unity`:

1. На Game-сцене явно повесить `GameLifetimeScope` (свой subclass с `Configure`)
2. В Inspector указать **Parent = RootLifetimeScope** (с Root-сцены)
3. **Auto Run выключить** — `Build()` вызывать из кода после additive load (например из `GameDirector`), по тому же принципу что Root + `Startup`

Так регистрация геймплея остаётся на сцене Game, а Root-сервисы (`IAudioManager`, `IGameEventBus`, `IGameDirector`) видны через parent container.

> **Нюанс:** ссылка Parent через сцены при additive иногда капризна — если Inspector-ссылка отвалится, parent можно проставить в коде перед `Build()` (`gameScope.Parent = rootScope` или API VContainer для parent).

См. также: [[04 Game Director]], [[05 Event Bus]].

## Что живёт на Root vs Game

| Root (вечно) | Game (контент сессии) |
| --- | --- |
| Startup, RootLifetimeScope | Слоты зданий, пирамида |
| AudioManager, EventBus | NPC на улице, карточки на экране |
| GameDirector | HUD конкретного поселения |
| Глобальный UI / диалоги (можно) | Таймлайн-визуал (кандидат) |
| Главный таймер Конца Света (кандидат) | |

Точное разбиение сущностей GDD по сценам — отдельная заметка, когда дойдём до геймплея.
