# 03 Audio Manager

← [[02 Scenes & Root LifetimeScope]] | [[Index]] | Далее → [[04 Game Director]]

## Решение

| Часть | Роль | Где живёт |
| --- | --- | --- |
| `IAudioManager` | контракт Play/Stop/Volume | `TheyWillDescend.Core` |
| `AudioManager` | MonoBehaviour: пулы, mixer, PlayerPrefs | `TheyWillDescend.UI`, объект на **Root** |
| `AudioCatalog` | ScriptableObject: `id → clip(s)` | `Core` + ассет в `_Project/Data/Audio/` |

Опционально позже: сервис, который слушает [[05 Event Bus|event bus]] и вызывает `_audio.Play("Id")`. **На старте не обязателен** — системы могут звать `IAudioManager` напрямую.

## Регистрация в DI

В родительском Root scope:

```csharp
builder.RegisterComponentInHierarchy<AudioManager>().As<IAudioManager>();
```

`AudioManager` лежит на сцене `Root.unity` → виден всем будущим child-scopes.

## Ключевой API

```csharp
public interface IAudioManager
{
    void Play(string soundId, float? pitch = null, float? pitchRandomRange = null);
    void Stop(string soundId);
    void StopMusic();
    void PauseMusic();
    void ResumeMusic();
    void StopAll();
    bool IsPlaying(string soundId);
    // volumes + warmup…
}
```

Идентификаторы звуков — **строковые id** из каталога (константы `AudioCatalog.Ids.*`), не прямые ссылки на `AudioClip` из геймплей-кода.

## Ассеты

```
_Project/
├── Audio/
│   ├── Music/
│   ├── SFX/
│   └── Mixers/          # AudioMixer (Music / SFX groups)
└── Data/
    └── Audio/
        └── AudioCatalog.asset
```

У `AudioManager` на Root: ссылки на `AudioCatalog` и `AudioMixer` (если используем группы громкости).

## Связь с GDD / атмосферой

Звук поддерживает напряжение Countdown ([[../GDD/00 Overview|Overview]], [[../GDD/04 Timeline & Events|Events]]):

| Момент | Пример id (черновик) |
| --- | --- |
| Фоновая музыка (main) | `MusicMain` |
| Фоновая музыка (мрачная) | `MusicDark` |
| Взял карту / карта в лоток | `CardPickup` |
| Успешный drop на здание | `CardDropOk` |
| Производство выдало ресурс | `ResoursGold` |
| Старт стройки (Constructing) | `BuildStart` |
| Здание построено (Built) | `BuildComplete` (кандидат) |
| Удар молнии | `Thunder` (`ThunderService`) |
| Поражение (позже) | `Defeat` |

Старт игры: `GameStartState` → `Play(MusicMain)` (loop в каталоге).

Удар: `RandomStrikeButton` → `IDisasterManager` (сцена, VFX) → `IThunderService` (SFX + disable).

## Доработка: привязка к BPM

Поверх базового менеджера нужно сделать **синхронизацию с BPM** трека (музыка / ритм отсчёта):

- хранить BPM у клипа или в `AudioCatalog` (метаданные записи)
- уметь отдавать наружу фазу бита / колбэки на beat (или тик в долю), чтобы геймплей и UI могли попадать в ритм
- кандидаты на привязку: тики таймера пирамиды, акценты катаклизмов, пульс HUD / Countdown-атмосфера

Детали API (события vs polling, offset/latency) — уточнить отдельно, когда появятся музыкальные ассеты. В минимальный `AudioManager` это **не входит** — отдельный слой поверх `IAudioManager` (и при желании публикация beat-событий в [[05 Event Bus]]).

## План внедрения (код позже)

1. `IAudioManager`, `AudioManager`, `AudioCatalog` (неймспейсы `TheyWillDescend.*`)
2. UI-звуки кнопок — по мере появления UI
3. Зарегистрировать в `RootLifetimeScope.Configure`
4. Позже: BPM-слой
