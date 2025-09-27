# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

This is a full-stack YouTube downloader application with:

- **Backend**: .NET 9.0 ASP.NET Core Web API with Clean Architecture
  - `YoutubeDownloader.WebApi` - API controllers, SignalR hubs, services
  - `YoutubeDownloader.Core` - Business logic, downloading, resolving, tagging
  - `YoutubeDownloader.Infrastructure` - Data access, Entity Framework, repositories
  - `YoutubeDownloader.Tests` - Unit and integration tests

- **Frontend**: React + TypeScript with Vite
  - Modern React with hooks, Zustand for state management
  - TailwindCSS for styling, Headless UI components
  - SignalR for real-time download progress
  - React Query for API calls, React Hook Form + Zod validation

- **Infrastructure**: Docker Compose with PostgreSQL, Redis, Nginx

## Development Commands

### Backend (.NET)

```bash
# Run from src/YoutubeDownloader.WebApi/
dotnet run                          # Start API (http://localhost:5000)
dotnet test                         # Run all tests
dotnet test --filter Category=Unit # Run unit tests only
dotnet ef database update           # Apply database migrations
dotnet build                        # Build solution
```

### Frontend (React)

```bash
# Run from frontend/
npm run dev                         # Start dev server
npm run build                       # Build for production
npm run lint                        # Run ESLint
npm run lint:fix                    # Fix ESLint issues
npm run type-check                  # TypeScript type checking
npm run test                        # Run Vitest tests
npm run test:coverage               # Run tests with coverage
npm run format                      # Format code with Prettier
```

### Docker Deployment

```bash
# Development environment with database only
docker-compose -f docker-compose.dev.yml up -d

# Full production deployment
chmod +x scripts/deploy.sh
./scripts/deploy.sh

# Or manually
docker-compose up -d
```

## Key Service URLs

- **API**: http://localhost:5000 (dev) or http://localhost:8080 (prod)
- **Frontend**: http://localhost:5173 (dev)
- **Swagger UI**: Available at API base URL
- **Hangfire Dashboard**: `/hangfire` endpoint
- **Health Check**: `/api/health`

## Tech Stack Details

**Backend**: Entity Framework Core + PostgreSQL, AutoMapper, FluentValidation, Serilog, Hangfire (background jobs), SignalR (real-time), xUnit testing

**Frontend**: React Router, Axios, TanStack Query, React Hook Form, Zod, Framer Motion, React Hot Toast, Vitest + Testing Library

## Testing Strategy

- Backend: 95%+ test coverage with unit and integration tests
- Frontend: Component testing with Vitest, accessibility tests, i18n tests
- Use `--filter Category=Unit` or `Category=Integration` for backend test filtering