---
id: web-umbraco-dotnet
label: Corporate Site (Umbraco + .NET)
tags: [web, cms, enterprise]
summary: Umbraco CMS 17 LTS on .NET 10 with SQL Server or SQLite and Docker-friendly setup
---

frontend: Razor views or React
backend: .NET 10 + Umbraco CMS 17 LTS
data/storage: SQL Server or SQLite (production ok for moderate load)
infra/deploy: Docker Compose (local dev) or Azure App Service (prod)
testing: Playwright + xUnit
observability: OpenTelemetry + Application Insights
avoid: Postgres or MySQL for Umbraco CMS core
