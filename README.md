# 🌐 Ping by Daylight

<div align="center">

![Version](https://img.shields.io/badge/version-1.0.4-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Framework](https://img.shields.io/badge/framework-.NET%209.0-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

**Клиент для управления регионами AWS/GameLift в Dead by Daylight**

[🚀 Скачать](https://github.com/Wafphlez/AWSServerSelector/releases) • [🐛 Сообщить об ошибке](https://github.com/Wafphlez/AWSServerSelector/issues/new/choose) • [💬 Discord](https://discord.gg/gnvtATeVc4)

</div>

---

## ✨ Что делает приложение

`Ping by Daylight` помогает управлять доступом к регионам матчмейкинга Dead by Daylight через секцию в системном `hosts`:

- измеряет задержку по регионам в реальном времени;
- позволяет выбрать нужные регионы для игры;
- применяет правила в `hosts` только в секции приложения;
- поддерживает быстрый откат (`Reset to default`);
- показывает уведомления и статусы без модальных окон.

## ⚙️ Основные возможности

- **Два режима применения:**
  - `Gatekeep` - блокирует невыбранные регионы (`0.0.0.0`), выбранные оставляет закомментированными;
  - `Universal Redirect` - перенаправляет все регионы на выбранный.
- **Гибкая фильтрация:** блокировка только `ping`, только `service` или обоих типов хостов.
- **Merge Unstable Servers:** автодобавление стабильной альтернативы для нестабильного региона (в рамках той же группы).
- **Поддержка локализации:** `en`/`ru`.
- **Дополнительные окна:** `Connection Info`, `Settings`, `Update`, `About`.

## 🧩 Системные требования

- Windows 10/11;
- .NET 9 Runtime;
- запуск с правами администратора (для изменения `hosts`).

## 🚀 Быстрый старт

1. Скачайте последнюю версию из [Releases](https://github.com/Wafphlez/AWSServerSelector/releases).
2. Запустите `PingByDaylight.exe` от имени администратора.
3. Выберите регионы.
4. Нажмите `Apply Selection`.

## 🌍 Поддерживаемые группы регионов

- Europe
- The Americas
- Asia (excluding Mainland China)
- Oceania
- Mainland China

Полный список задается в `Config/regions.json` и может обновляться без изменений кода.

## 🛡️ Как выглядит секция в hosts

Ниже упрощенный пример содержимого, которое генерируется приложением:

```hosts
# -- Ping by Daylight --
# Edited by Ping by Daylight
# Unselected servers are blocked (Gatekeep Mode); selected servers are commented out.
# Need help? Discord: https://discord.gg/gnvtATeVc4

# Europe
# gamelift.eu-west-1.amazonaws.com
0.0.0.0   gamelift.eu-west-2.amazonaws.com
```

## 🧠 Конфигурация

Основные параметры:

- `appsettings.json`
  - `Update` - API обновлений, таймауты, user-agent;
  - `Monitoring` - интервалы и таймауты ping/опроса;
  - `Hosts` - путь к шаблону hosts по умолчанию.
- `Config/regions.json` - каталог регионов и их host-эндпоинты.
- `Config/default-hosts.txt` - шаблон hosts для сброса.

Пользовательские настройки сохраняются в:

```text
%APPDATA%\Wafphlez\PingByDaylight\settings.json
```

## 🛠️ Разработка

### Сборка и запуск

```bash
git clone https://github.com/Wafphlez/AWSServerSelector.git
cd AWSServerSelector
dotnet restore
dotnet build
dotnet run --project AWSServerSelector.csproj
```

### Тесты

```bash
dotnet test
```

### Технологии

- WPF + MVVM (`.NET 9`);
- `Microsoft.Extensions.DependencyInjection` для DI;
- `IOptions` для валидируемой конфигурации;
- сервисный слой для работы с сетью, hosts и UI-уведомлениями.

## ✅ Проверка после изменений

Рекомендуемый smoke-чеклист: `docs/smoke-checklist.md`.

Ключевые сценарии:

- Apply/Revert на главном окне;
- Settings Apply;
- окно Connection Info;
- проверка обновлений (Update dialog).

## 🤝 Вклад в проект

1. Форкните репозиторий.
2. Создайте ветку: `git checkout -b feature/my-change`.
3. Сделайте изменения и запустите `dotnet build` + `dotnet test`.
4. Откройте Pull Request.

## 📄 Лицензия

MIT - подробнее в файле [LICENSE](LICENSE).

## 🔗 Полезные ссылки

- Issues: [https://github.com/Wafphlez/AWSServerSelector/issues](https://github.com/Wafphlez/AWSServerSelector/issues)
- Releases: [https://github.com/Wafphlez/AWSServerSelector/releases](https://github.com/Wafphlez/AWSServerSelector/releases)
- Discord: [https://discord.gg/gnvtATeVc4](https://discord.gg/gnvtATeVc4)

## 🙏 Благодарности

- Оригинальный проект: [make-your-choice](https://codeberg.org/ky/make-your-choice)

---

<div align="center">

**Сделано с ❤️ для Dead by Daylight сообщества**

[⬆ Наверх](#-ping-by-daylight)

</div>
