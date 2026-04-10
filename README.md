# 🌐 Ping by Daylight

<div align="center">

![Version](https://img.shields.io/badge/version-1.0.3-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Framework](https://img.shields.io/badge/framework-.NET%209.0-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

**Удобный клиент для выбора AWS серверов с минимальной задержкой**

[🚀 Скачать](https://github.com/Wafphlez/AWSServerSelector/releases) • [📖 Документация](#документация) • [🐛 Сообщить об ошибке](https://github.com/Wafphlez/AWSServerSelector/issues/new/choose)

</div>

---

## ✨ Особенности

- 🎯 **Интеллектуальный выбор серверов** - автоматический выбор лучшего сервера на основе задержки
- 🌍 **Выбор из всех доступных серверов** - серверы в Европе, Америке, Азии, Океании и Китае
- ⚡ **Реальное время** - мониторинг задержки каждые 5 секунд
- 🎨 **Современный интерфейс** - красивый WPF-интерфейс с темной темой
- 🔧 **Гибкие настройки** - два режима работы: Gatekeep и Universal Redirect
- 📊 **Визуальная индикация** - цветовая индикация качества соединения
- 🛡️ **Безопасность** - работа с системным hosts файлом

## 🖼️ Скриншоты

<div align="center">

![AWS Server Selector Interface](https://via.placeholder.com/500x400/0D1117/FFFFFF?text=AWS+Server+Selector+Interface)

*Современный интерфейс с группировкой серверов по регионам*

</div>

## 🚀 Быстрый старт

### Системные требования

- **ОС:** Windows 10 или выше
- **Framework:** .NET 9.0 Runtime
- **Права:** Администратор (для изменения hosts файла)

### Установка

1. **Скачайте** последнюю версию из [Releases](../../releases)
2. **Запустите** `AWSServerSelector.exe` от имени администратора
3. **Выберите** нужные серверы и нажмите "Apply Selection"

## 📖 Документация

### Режимы работы

#### 🔒 Gatekeep Mode (Рекомендуется)
- Блокирует невыбранные серверы
- Позволяет выбрать несколько серверов
- Система автоматически выберет лучший

#### 🌐 Universal Redirect Mode
- Перенаправляет все запросы на выбранный сервер
- Требует выбора только одного сервера
- Подходит для принудительного использования конкретного региона

### Настройки

| Параметр | Описание |
|----------|----------|
| **Merge Unstable Servers** | Автоматически добавляет стабильные серверы для нестабильных регионов |
| **Block Mode** | Выбор блокируемых эндпоинтов (Ping, Service, или оба) |

### Поддерживаемые регионы

<details>
<summary>🌍 <strong>Европа</strong></summary>

- 🇬🇧 Europe (London)
- 🇮🇪 Europe (Ireland) ⭐
- 🇩🇪 Europe (Frankfurt am Main) ⭐

</details>

<details>
<summary>🌎 <strong>Америка</strong></summary>

- 🇺🇸 US East (N. Virginia) ⭐
- 🇺🇸 US East (Ohio)
- 🇺🇸 US West (N. California) ⭐
- 🇺🇸 US West (Oregon) ⭐
- 🇨🇦 Canada (Central)
- 🇧🇷 South America (São Paulo) ⭐

</details>

<details>
<summary>🌏 <strong>Азия (исключая Китай)</strong></summary>

- 🇯🇵 Asia Pacific (Tokyo) ⭐
- 🇰🇷 Asia Pacific (Seoul) ⭐
- 🇮🇳 Asia Pacific (Mumbai) ⭐
- 🇸🇬 Asia Pacific (Singapore) ⭐
- 🇭🇰 Asia Pacific (Hong Kong) ⭐

</details>

<details>
<summary>🌊 <strong>Океания</strong></summary>

- 🇦🇺 Asia Pacific (Sydney) ⭐

</details>

<details>
<summary>🇨🇳 <strong>Китай</strong></summary>

- 🇨🇳 China (Beijing) ⭐
- 🇨🇳 China (Ningxia) ⭐

</details>

> ⭐ = Стабильный сервер

## 🎨 Цветовая индикация

| Цвет | Задержка | Описание |
|------|----------|----------|
| 🟢 Зеленый | < 80ms | Отличное соединение |
| 🟠 Оранжевый | 80-130ms | Хорошее соединение |
| 🔴 Красный | 130-250ms | Удовлетворительное соединение |
| 🟣 Фиолетовый | > 250ms | Плохое соединение |
| ⚫ Серый | Нет соединения | Сервер недоступен |

## 🔧 Техническая информация

### Архитектура

- **Frontend:** WPF (.NET 9.0)
- **Backend:** C# с async/await
- **Networking:** System.Net.Ping для мониторинга
- **Storage:** JSON для настроек пользователя

### Файлы конфигурации

```
%APPDATA%\Wafphlez\PingByDaylight\settings.json
```

### Структура hosts файла

#### 🔒 Gatekeep Mode (Рекомендуется)

```hosts
# -- Ping by Daylight --
# Edited by Ping by Daylight
# Unselected servers are blocked (Gatekeep Mode)

# Разрешенные серверы (закомментированы)
# 1.2.3.4 gamelift.us-east-1.amazonaws.com
# 1.2.3.4 gamelift-ping.us-east-1.api.aws

# Заблокированные серверы
0.0.0.0 gamelift.eu-west-2.amazonaws.com
0.0.0.0 gamelift-ping.eu-west-2.api.aws
# --+ Make Your Choice +--
```

## 🛠️ Разработка

### Сборка из исходников

```bash
git clone https://github.com/Wafphlez/AWSServerSelector.git
cd AWSServerSelector
dotnet restore
dotnet build
dotnet run --project AWSServerSelector.csproj
```

### Требования для разработки

- Visual Studio 2022 или VS Code
- .NET 9.0 SDK
- Git

## 🤝 Вклад в проект

Мы приветствуем вклад в развитие проекта! Пожалуйста:

1. Форкните репозиторий
2. Создайте ветку для новой функции (`git checkout -b feature/AmazingFeature`)
3. Зафиксируйте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте в ветку (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE) для подробностей.

## 🆘 Поддержка

- 🐛 **Сообщить об ошибке:** [Issues](../../issues)
- 💬 **Discord:** [https://discord.gg/gnvtATeVc4](https://discord.gg/gnvtATeVc4)
- 🌐 **Веб-сайт:** [https://kurocat.net](https://kurocat.net)

## 🙏 Благодарности

- **Разработчик:** WAFPHLEZ
- **Оригинальный проект:** [make-your-choice](https://codeberg.org/ky/make-your-choice)
- **Сообщество:** Всем пользователям за обратную связь и предложения

---

<div align="center">

**Сделано с ❤️ для Dead by Daylight сообщества**

[⬆ Наверх](#-aws-server-selector)

</div>
