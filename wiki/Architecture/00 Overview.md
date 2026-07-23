# 00 Overview

← [[Index]] | Далее → [[01 Folder Structure]]

## Цель

Каркас джем-игры [[../GDD/00 Overview|They will descend]]: вечный Root, DI, аудио, оркестратор сессии и шина событий — без лишней сложности (отдельные меню-машины, лидерборды и т.п. не тащим).

## Стек (базовый)

| Слой | Решение |
| --- | --- |
| DI | VContainer + `LifetimeScope` |
| Bootstrap | сцена `Root` + `Startup` |
| Оркестрация | `IGameDirector` / `GameDirector` |
| Сообщения | `IGameEventBus` |
| Аудио | `IAudioManager` / `AudioManager` + `AudioCatalog` |
| Контент | всё своё в `Assets/_Project/` |
| Платформа | WebGL / itch.io — [[../GDD/00 Overview\|GDD]] |

## Слои кода (целевые asmdef)

```
TheyWillDescend.Core     — интерфейсы, константы сцен, аудио-контракты, шина, каталоги
TheyWillDescend.Main     — Startup, RootLifetimeScope, GameLifetimeScope, GameDirector
TheyWillDescend.Gameplay — поселение, здания, NPC, карточки, пирамида, таймлайн
TheyWillDescend.UI       — AudioManager; Overlay (таймлайн, карты, стройка); World HUD зданий
```

`Core` не ссылается на `Main` / `Gameplay`. `Gameplay` и `UI` получают сервисы через DI / события.

## Базовый каркас (первая оснастка)

1. Папка контента `_Project` — [[01 Folder Structure]]
2. Вечная `Root.unity` + родительский `RootLifetimeScope` — [[02 Scenes & Root LifetimeScope]]
3. `AudioManager` + `IAudioManager` + `AudioCatalog` — [[03 Audio Manager]]
4. `GameDirector` — [[04 Game Director]]
5. `IGameEventBus` — [[05 Event Bus]]

## Что откладываем

- Полная цепочка Root → App → Game child scopes (на старте достаточно Root; child Game — когда появится additive `Game`)
- Отдельный `AudioService` только на шине (сначала можно звать `IAudioManager` напрямую + точечные подписки)
- Overlay-навигация, PauseCoordinator, локализация, аналитика

## Принципы (коротко)

- Сцена Root **не выгружается** — глобальные сервисы живут там
- DI: регистрация прямо в `RootLifetimeScope.Configure` (без Installer SO; extensions — только если scope'ов станет много)
- Системы не дергают view напрямую — через [[05 Event Bus|шину]]
- Сцены / Inspector / `.meta` — только руками в Unity; код и доки — здесь
- Сначала согласовать подход в вики, потом писать код
