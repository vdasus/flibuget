# flibuget.Infrastructure

Infrastructure layer for the flibuget application. Contains concrete implementations of interfaces defined in `flibuget.Core`.

## Project Structure

```
flibuget.Infrastructure/
├── AI/                         # AI provider implementations
│   ├── AIProviderFactory.cs    # Factory for creating AI provider instances
│   ├── OpenAIProvider.cs       # OpenAI implementation (Azure.AI.OpenAI SDK)
│   ├── PerplexityProvider.cs   # Perplexity implementation (HTTP client)
│   └── README.md               # AI infrastructure documentation
├── AudioTags/                  # Audio file metadata handling
│   ├── AudioTagService.cs      # TagLibSharp-based tag read/write service
│   └── AudioTagServiceExtensions.cs
├── Configuration/              # DI registration extensions
│   └── AIServiceExtensions.cs  # AddAIServices() extension method
├── Data/                       # Data access implementations
│   ├── DataRepository.cs       # Generic Dapper repository
│   ├── SqliteConnectionFactory.cs
│   └── UnitOfWork.cs
├── Examples/                   # Usage examples (not production code)
│   ├── AIServiceUsageExample.cs
│   └── PerplexityApiUsageExample.cs
└── Http/
    └── HttpService.cs          # IWebService HTTP client wrapper
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.AI.OpenAI` | 2.1.0 | OpenAI API client |
| `Dapper` | 2.1.72 | Micro-ORM for SQLite queries |
| `Microsoft.Data.Sqlite` | 10.0.7 | SQLite database driver |
| `Microsoft.Extensions.Configuration` | 10.0.7 | Configuration abstraction |
| `Microsoft.Extensions.DependencyInjection` | 10.0.7 | DI container |
| `Microsoft.Extensions.Http` | 10.0.7 | HttpClientFactory |
| `Microsoft.Extensions.Logging` | 10.0.7 | Logging abstraction |
| `System.IO.Abstractions` | 22.1.1 | File system abstraction (testability) |
| `TagLibSharp` | 2.3.0 | Audio file tag reading/writing |

## Registration

All infrastructure services are registered via extension methods in `Configuration/AIServiceExtensions.cs`.
In `CompositionRoot.cs`:

```csharp
services.AddAIServices(configuration);
```

## Related Documentation

- AI Providers: [`AI/README.md`](AI/README.md)
- AudioTag Service: [`../docs/TagLib.md`](../docs/TagLib.md)
- Core interfaces: `flibuget.Core/InfraServices/`
