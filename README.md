# Universal Automix — Stage 0 (source)

Это *исходники* Stage 0: каркас приложения + UI-макет (без реальной логики/детекта/подключения).

## Требования
- Windows 10/11
- .NET 8 **SDK** (не runtime): https://dotnet.microsoft.com/download/dotnet/8.0
- Интернет для NuGet restore (Avalonia packages)

## Запуск
Открой PowerShell в папке проекта и выполни:

```powershell
dotnet restore
dotnet run --project .\src\Automix.App\Automix.App.csproj
```

## Сборка в один exe (Windows x64)
```powershell
dotnet publish .\src\Automix.App\Automix.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Готовый exe будет в:
`src\Automix.App\bin\Release\net8.0\win-x64\publish\`
