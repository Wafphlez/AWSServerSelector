# Baseline Quality Snapshot

## Команды
- `dotnet restore`
- `dotnet build AWSServerSelector.sln`
- `dotnet test AWSServerSelector.sln`

## Текущее состояние
- Сборка: успешна (`OutDir=artifacts/build`, чтобы обойти lock запущенного exe).
- Ошибки: отсутствуют.
- Тесты: 11/11 успешны.
- Предупреждения: отсутствуют в текущем baseline-проходе.

## Цель для рефакторинга
- На каждом этапе не допускать новых ошибок сборки.
- Постепенно снижать количество предупреждений, особенно в hot paths.
