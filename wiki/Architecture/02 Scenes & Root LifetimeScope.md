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
│  ├─ EventSystem / Global Overlay Canvas      │
│  │    TOP: таймлайн + день                   │
│  │    BOTTOM: карты + меню стройки           │
│  └─ (опционально) loading screen             │
├──────────────────────────────────────────────┤
│  Game.unity  (additive)                      │
│  └─ поселение, слоты, пирамида               │
│     + World Space HUD зданий + drop-zones    │
│     + child LifetimeScope                    │
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
  → Resolve IGameDirector → StartAsync…
```

Псевдокод:

```csharp
// Startup
rootScope.Build();
var director = rootScope.Container.Resolve<IGameDirector>();
await director.StartAsync();
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

Child-scopes: [[#План: GameLifetimeScope на сцене|GameLifetimeScope]] грузится additive, parent = Root.

### План: GameLifetimeScope на сцене

1. На Game-сцене повесить `GameLifetimeScope` (Auto Run **off**)
2. Parent проставляется **в коде** из `GameDirector` перед `Build()` (надёжнее cross-scene Inspector)
3. Опционально в Inspector: Parent Type = `RootLifetimeScope`
4. `GameDirector.StartAsync` → LoadScene Additive → Find scope → `Build()` → `GameStartState.Enter()`

См. также: [[04 Game Director]], [[05 Event Bus]].

## Что живёт на Root vs Game

| Root (вечно) | Game (контент сессии) |
| --- | --- |
| Startup, RootLifetimeScope | Слоты зданий, пирамида |
| AudioManager, EventBus | NPC на улице |
| GameDirector | **World Space** HUD зданий + drop-zones |
| **Overlay**: top таймлайн/день, bottom карты + стройка | Мир поселения (спрайты) |
| Главный таймер Конца Света (кандидат) | |

> Карточки и таймлайн — Screen Space Overlay на Root. HUD зданий — World Space на Game. См. [[../GDD/06 UI & Visual|GDD UI]].

Точное разбиение сущностей GDD по сценам — отдельная заметка, когда дойдём до геймплея.
