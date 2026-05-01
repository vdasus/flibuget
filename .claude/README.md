---
tags: [claude, setup, dotnet]
---

# Claude Code: стартовый комплект для .NET-архитектора

## Структура

```
claude/
├── global/CLAUDE.md                  → ~/.claude/  (персональный профиль)
└── project-template/
    ├── CLAUDE.md                     (шаблон проектного контекста)
    └── claude-config/                → .claude/  при копировании в проект
        ├── docs/dotnet-standards.md
        ├── agents/{code-reviewer,architecture-analyzer,perf-analyzer}.md
        └── commands/review.md        (/review slash-команда)
```

> `claude-config` → переименовать в `.claude` при копировании в репозиторий.

## Установка

**Глобальный профиль (PowerShell):**
```powershell
$dest = "$env:USERPROFILE\.claude"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item "global\CLAUDE.md" -Destination "$dest\CLAUDE.md" -Force
```

**Шаблон в проект (в корне репозитория):**
```powershell
$src = "<путь>\project-template"
Copy-Item "$src\CLAUDE.md" -Destination ".\CLAUDE.md" -Force
Copy-Item "$src\claude-config" -Destination ".\.claude" -Recurse -Force
```

После копирования: заполни плейсхолдеры в `CLAUDE.md`, подправь `dotnet-standards.md` под проект.

## Использование

- `/review` — ревью текущих изменений (запускает три subagent-а параллельно)
- `code-reviewer / architecture-analyzer / perf-analyzer` — вызов агента напрямую в чате
- `dotnet-standards.md` подключается через `@.claude/docs/dotnet-standards.md` — грузится по запросу, не каждое сообщение

## Файлы и частота загрузки

| Файл | Назначение | Загружается |
|------|-----------|-------------|
| `~/.claude/CLAUDE.md` | персональный профиль | каждый чат |
| `<repo>/CLAUDE.md` | контекст проекта | каждое сообщение |
| `dotnet-standards.md` | детальные правила | по `@`-ссылке |
| `agents/*.md` | специализированные ревью | по вызову / автоматически |
| `commands/review.md` | `/review` | по команде |

## Что расширять

- Команды: `/new-feature`, `/add-migration`, `/adr`, `/refactor`
- Агенты: `security-auditor`, `db-schema-reviewer`, `api-contract-checker`
- `.claude/settings.json` хуки: `PostToolUse` на `Edit/Write *.cs` → `dotnet format`, `PreToolUse` → блокировка `rm -rf`, `drop`
