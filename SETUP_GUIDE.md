# Theft of Artefact — Пошаговая инструкция настройки

## Этап 1. Подготовка проекта

### 1.1 Установка Mirror

После открытия проекта в Unity, Mirror должен автоматически загрузиться из Git-репозитория (см. `Packages/manifest.json`).

**Если Mirror не установился автоматически:**
1. Откройте **Window → Asset Store** (или Unity Asset Store в браузере)
2. Найдите "Mirror" от vis2k
3. Нажмите **Import** → **Import All**

**Проверка:** В папке `Packages` в окне Project должен появиться пакет Mirror.

---

### 1.2 Структура папок

Структура уже создана:
```
Assets/
├── Scripts/
│   ├── Network/        ← NetworkManagerSetup.cs
│   ├── Player/         ← PlayerController.cs
│   └── UI/             ← ConnectionUI.cs
├── Prefabs/            ← сюда сохраним PlayerPrefab
├── Materials/
└── Scenes/             ← MainScene
```

---

## Этап 2. Настройка MainScene

### 2.1 Создание сцены

1. **File → New Scene → Basic (Built-in)**
2. **File → Save As** → `Assets/Scenes/MainScene.unity`
3. Если есть `SampleScene` — можно удалить или переименовать.

### 2.2 Создание пола

1. **GameObject → 3D Object → Plane**
2. В Inspector установите:
   - Position: `(0, 0, 0)`  
   - Scale: `(5, 1, 5)` — получится пол 50×50 метров.
3. Переименуйте в **"Floor"**.

### 2.3 Проверка камеры и света

Сцена уже должна содержать:
- **Main Camera** — можно удалить (камера будет у каждого игрока в префабе)
- **Directional Light** — оставить как есть

> **Важно:** Удалите Main Camera со сцены! Камера будет создаваться автоматически вместе с каждым игроком.

---

## Этап 3. Создание PlayerPrefab

### 3.1 Создание объекта игрока

1. **GameObject → Create Empty** → переименуйте в **"Player"**
2. Установите Position: `(0, 1, 0)`

### 3.2 Добавление тела (визуал)

1. Выберите **Player** → **GameObject → 3D Object → Capsule** (как дочерний)
2. Capsule будет автоматически создан как потомок Player
3. Переименуйте в **"Body"**
4. Body Position: `(0, 0, 0)` (локальная)

### 3.3 Добавление камеры

1. Выберите **Player** → **GameObject → Camera** (как дочерний объект)
2. Переименуйте камеру в **"PlayerCamera"**
3. Установите локальную позицию камеры: `(0, 0.8, 0)` — на уровне "головы" капсулы.
4. **Отключите камеру** (снимите галочку в Inspector) — скрипт включит её только для локального игрока.

### 3.4 Добавление компонентов на Player

На корневой объект **Player** добавьте:

1. **Character Controller** (Add Component → Character Controller)
   - Height: `2`
   - Center: `(0, 0, 0)`
   - Radius: `0.5`

2. **Network Identity** (Add Component → Network Identity)
   - Оставьте настройки по умолчанию

3. **Network Transform Reliable** (Add Component → Network Transform Reliable)
   - Это синхронизирует позицию и вращение между клиентами

4. **PlayerController** (Add Component → PlayerController)
   - Скрипт из `Assets/Scripts/Player/PlayerController.cs`

### 3.5 Сохранение как Prefab

1. Перетащите объект **Player** из Hierarchy в папку `Assets/Prefabs/`
2. Появится окно — выберите **"Original Prefab"**
3. **Удалите Player из сцены** (со сцены, не из Prefabs!)

---

## Этап 4. Настройка Network Manager

### 4.1 Создание объекта NetworkManager

1. **GameObject → Create Empty** → переименуйте в **"NetworkManager"**
2. Добавьте компоненты:
   - **NetworkManagerSetup** (Add Component → NetworkManagerSetup)  
     _(это наш кастомный скрипт, НЕ стандартный Network Manager)_
   - **Kcp Transport** (Add Component → Kcp Transport)  
     _(транспортный протокол Mirror)_

### 4.2 Настройка NetworkManagerSetup

В Inspector компонента **NetworkManagerSetup**:

1. **Player Prefab**: перетащите `Assets/Prefabs/Player.prefab` в это поле
2. **Transport**: перетащите компонент **Kcp Transport** (с этого же объекта) в поле Transport
3. **Network Address**: оставьте `localhost`
4. **Auto Create Player**: ✅ включено (по умолчанию)

---

## Этап 5. Создание UI подключения

### 5.1 Создание Canvas

1. **GameObject → UI → Canvas**
2. Canvas Scaler → UI Scale Mode: **Scale With Screen Size**
3. Reference Resolution: `1920 x 1080`

### 5.2 Создание панели подключения

1. Правый клик на **Canvas** → **UI → Panel** → переименуйте в **"ConnectionPanel"**
2. На панели установите:
   - Anchor: Middle Center
   - Width: `400`, Height: `350`
   - Color: `(0, 0, 0, 200)` — полупрозрачный чёрный фон

### 5.3 Добавление заголовка

1. Правый клик на **ConnectionPanel** → **UI → Text - TextMeshPro**
2. Переименуйте в **"Title"**
3. Text: **"Theft of Artefact"**
4. Font Size: `28`, Alignment: Center
5. Anchor: Top Center, Pos Y: `-30`

### 5.4 Поле ввода IP-адреса

1. Правый клик на **ConnectionPanel** → **UI → Input Field - TextMeshPro**
2. Переименуйте в **"IPInput"**
3. Placeholder text: `"Введите IP-адрес..."`
4. Anchor: Middle Center, Pos Y: `40`
5. Width: `300`, Height: `40`

### 5.5 Кнопка Host

1. Правый клик на **ConnectionPanel** → **UI → Button - TextMeshPro**
2. Переименуйте в **"HostButton"**
3. Текст дочернего Text: **"Host (Сервер + Клиент)"**
4. Anchor: Middle Center, Pos Y: `-10`
5. Width: `300`, Height: `40`

### 5.6 Кнопка Connect

1. Правый клик на **ConnectionPanel** → **UI → Button - TextMeshPro**
2. Переименуйте в **"ConnectButton"**
3. Текст: **"Connect as Client"**
4. Pos Y: `-60`
5. Width: `300`, Height: `40`

### 5.7 Текст статуса

1. Правый клик на **ConnectionPanel** → **UI → Text - TextMeshPro**
2. Переименуйте в **"StatusText"**
3. Text: **"Не подключено"**
4. Font Size: `16`, Alignment: Center
5. Pos Y: `-110`

### 5.8 Кнопка Disconnect

1. Правый клик на **Canvas** → **UI → Button - TextMeshPro** *(НЕ в панели, а на самом Canvas!)*
2. Переименуйте в **"DisconnectButton"**
3. Текст: **"Disconnect"**
4. Anchor: Top Right, Pos: `(-100, -30)`
5. Width: `180`, Height: `40`

### 5.9 Добавление скрипта ConnectionUI

1. На объект **Canvas** добавьте компонент **ConnectionUI**
2. Заполните поля в Inspector:
   - **Connection Panel** → перетащите `ConnectionPanel`
   - **IP Address Input** → перетащите `IPInput`
   - **Host Button** → перетащите `HostButton`
   - **Connect Button** → перетащите `ConnectButton`
   - **Disconnect Button** → перетащите `DisconnectButton`
   - **Status Text** → перетащите `StatusText`

---

## Этап 6. Настройка Build Settings

1. **File → Build Settings**
2. Нажмите **Add Open Scenes** (должна быть MainScene)
3. Target Platform: **PC, Mac & Linux Standalone**
4. Architecture: **x86_64**
5. Нажмите **Build** → выберите папку для сборки

---

## Этап 7. Тестирование

### 7.1 Тест Host (локальный)

1. Нажмите **Play** в редакторе Unity
2. Нажмите кнопку **"Host"** на экране
3. **Ожидаемый результат:**
   - Панель подключения скрылась
   - Появилась кнопка Disconnect
   - На сцене появился персонаж (капсула)
   - В Console видны логи:
     ```
     [Server] Сервер запущен.
     [Host] Хост запущен (сервер + клиент).
     [Client] Успешное подключение к серверу!
     [Server] Игрок добавлен для соединения: 0. Всего игроков: 1
     ```
   - Можно двигаться WASD + мышь

### 7.2 Тест двух игроков

1. **Сначала** соберите Build (File → Build And Run или Build)
2. Запустите собранное приложение (.exe)
3. В собранном приложении нажмите **Host**
4. В редакторе Unity нажмите **Play**, затем **"Connect as Client"** (IP: `localhost`)
5. **Ожидаемый результат:**
   - На обоих экранах видны два персонажа (разных цветов)
   - Оба могут двигаться независимо
   - Движения синхронизируются между инстансами
   - В Console хоста видно:
     ```
     [Server] Клиент подключился: ...
     [Server] Игрок добавлен для соединения: 1. Всего игроков: 2
     ```

### 7.3 Тест отключения

1. В редакторе нажмите **Disconnect**
2. **Ожидаемый результат:**
   - Персонаж клиента исчезает с экрана хоста
   - В Console хоста:
     ```
     [Server] Клиент отключился: ...
     ```
   - Панель подключения снова видна в редакторе

---

## Частые проблемы

| Проблема | Решение |
|----------|---------|
| Mirror не установился | Установите вручную из Asset Store (Mirror by vis2k) |
| Ошибка "No Transport" | Добавьте Kcp Transport на объект NetworkManager |
| Игрок не спавнится | Проверьте Player Prefab в NetworkManager и Network Identity на префабе |
| Камера не работает | Проверьте что PlayerCamera — дочерний объект Player и отключена по умолчанию |
| Два игрока двигаются синхронно | Проверьте `if (!isLocalPlayer) return;` в PlayerController |
| UI не скрывается | Проверьте привязку полей в ConnectionUI компоненте |
