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

## Definition of Done (этапный)
- `dotnet restore`, `dotnet build`, `dotnet test` проходят после каждого этапа.
- Новая вынесенная логика покрыта unit-тестами до интеграции в UI.
- `Windows/*.xaml.cs` не содержит прямых `Process.Start` / `MessageBox.Show` / `Clipboard.SetText` без сервисного адаптера.
- UI-изменения подтверждаются smoke-сценариями из `docs/smoke-checklist.md`.
