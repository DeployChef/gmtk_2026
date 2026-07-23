# 01 Folder Structure

← [[00 Overview]] | [[Index]] | Далее → [[02 Scenes & Root LifetimeScope]]

## Принцип

Всё игровое содержимое — в **`Assets/_Project/`** (префикс `_` поднимает папку вверх в Project window).

**Не кладём** в корень `Assets/` разрозненные `Scripts/`, `Sprites/`, `Audio/` — только `_Project`, плюс сторонние пакеты (`Plugins/`, URP Settings, TextMesh Pro и т.п.).

## Целевое дерево

```
Assets/
├── _Project/
│   ├── Code/
│   │   ├── TheyWillDescend.Core/
│   │   ├── TheyWillDescend.Main/
│   │   ├── TheyWillDescend.Gameplay/
│   │   ├── TheyWillDescend.UI/
│   │   └── TheyWillDescend.Editor/          # по необходимости
│   │
│   ├── Scenes/
│   │   ├── Root.unity                       # единственная в Build Settings (на старте)
│   │   └── Game.unity                       # позже: additive геймплей поселения
│   │
│   ├── Prefabs/
│   │   ├── Systems/                         # Audio, UI shells…
│   │   ├── Buildings/
│   │   ├── Cards/
│   │   └── NPC/
│   │
│   ├── Art/
│   │   ├── Sprites/
│   │   ├── Animations/
│   │   ├── Materials/
│   │   └── Fonts/
│   │
│   ├── Audio/
│   │   ├── Music/
│   │   ├── SFX/
│   │   └── Mixers/
│   │
│   ├── Data/                                # ScriptableObjects (баланс, каталоги)
│   │   └── Audio/                           # AudioCatalog и т.п.
│   │
│   └── Settings/                            # Input Actions и пр.
│
├── Plugins/                                 # VContainer, UniTask… (если не через UPM)
└── … (пакеты Unity)
```

## Соответствие asmdef и папок Code

| Папка | Ответственность |
| --- | --- |
| `TheyWillDescend.Core` | `IAudioManager`, `AudioCatalog`, `IGameEventBus`, имена сцен, общие типы без MonoBehaviour-логики игры |
| `TheyWillDescend.Main` | `Startup`, `RootLifetimeScope`, `GameDirector` |
| `TheyWillDescend.Gameplay` | здания, NPC, карточки, пирамида, таймлайн — см. GDD |
| `TheyWillDescend.UI` | `AudioManager`, HUD зданий, сайдбар стройки, диалоги Шамана |

## Сцены vs ассеты

| Что | Где | Кто создаёт |
| --- | --- | --- |
| `Root.unity`, слоты зданий, привязки Inspector | Unity Editor | человек |
| C# в `Code/` | репозиторий | агент после согласования |
| `.meta` | Unity | человек / Unity, агент не правит вручную |

## Связь с GDD

Слоты строительства захардкожены на сцене ([[../GDD/03 Economy|Economy]]) → контент уровня живёт в `Scenes/Game.unity` (или временно на Root), а не спавнится «где угодно» из кода.
