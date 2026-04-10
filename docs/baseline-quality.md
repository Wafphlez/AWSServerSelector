# Baseline Quality Snapshot

## Команды
- `dotnet restore`
- `dotnet build AWSServerSelector.sln`

## Текущее состояние
- Сборка: успешна.
- Ошибки: отсутствуют.
- Предупреждения: есть nullable-предупреждения в `Windows/MainWindow.xaml.cs`.

## Цель для рефакторинга
- На каждом этапе не допускать новых ошибок сборки.
- Постепенно снижать количество предупреждений, особенно в hot paths.
