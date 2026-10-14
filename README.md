# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Both pass on a clean checkout and neither needs a database. SQLite query tests cover the
`NotApplicable`, `Met`, and `Breached` SLA filters. `AtRisk` and `WithinSla` use SQL Server's
`DateDiffSecond` translation and are verified against the SQL Server API; they cannot run in
SQLite. An SQLite repository test also covers `AgentRepository.GetCandidatesAsync`: grouped
open-ticket counts exclude resolved/closed tickets and can omit the ticket currently being
escalated.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## Task 4 - Dockerisation & Production Strategy

The API image is built from the repository root so its .NET project references and central
package configuration are available:

```bash
cp .env.example .env
docker compose up --build
```

Compose starts SQL Server and waits for its health check before starting the API. The API is
available at `http://localhost:5080`; its anonymous `GET /health` endpoint checks database
reachability. In the container, `ASPNETCORE_ENVIRONMENT=Development` applies migrations and
seeds the demo data before the API accepts requests. Set `MSSQL_SA_PASSWORD` and `Jwt__Key` in
`.env` for local overrides; the example values are development fixtures only. To point the web
dev server at this container, run `VITE_API_PROXY_TARGET=http://localhost:5080 npm run web`.

The multi-stage Dockerfile restores from the copied project/build configuration before copying
source, then publishes into the ASP.NET runtime image. The final image runs as the non-root `app`
user, exposes HTTP port 8080, and uses `/health` for its container health check. A local build
produced a 493 MB runtime image (492,855,542 bytes; `docker image ls northwind-support-api`).
Compare it with the SDK image using `docker image ls mcr.microsoft.com/dotnet/sdk`.

### Docker Build & Verification

- **Non-root user:** The runtime image uses ASP.NET's `app` user and listens on port 8080 inside
  the container. The intended runtime check is `docker compose exec api whoami` (`app`).
- **Health check:** Anonymous `GET /health` uses `db.Database.CanConnectAsync()` to report
  database connectivity. It is also the Docker health check.
- **Image size:** The locally built `northwind-support-api:latest` image measured
  492,855,542 bytes (about 493 MB) with `docker image inspect`. The Docker daemon was unavailable
  when attempting to measure the SDK and base ASP.NET images; no estimated image sizes are
  presented as measurements.
- **Layer caching:** The Dockerfile copies shared build configuration and project files and
  restores packages before copying source code. `docker compose build api` succeeded once; the
  follow-up source-edit/cache-hit check has not yet been verified.
- **Compose startup:** `docker compose config --quiet` succeeded. A clean-volume end-to-end
  startup and HTTP checks remain unverified because Docker Desktop reported that it could not
  start and its containerd metadata filesystem was read-only during API container creation.
  At the time of checking, the host data volume had only 8.5 GiB free (96% used), which may
  have contributed; the Docker failure was not confirmed to be caused by disk space. The
  existing SQL volume was preserved.

When Docker is available, verify a disposable clean database with
`docker compose down -v && docker compose up --build -d`, then run:

```bash
curl -i http://localhost:5080/health
curl -i http://localhost:5080/api/tickets
docker compose exec api whoami
VITE_API_PROXY_TARGET=http://localhost:5080 npm run web
docker image ls | grep -E 'dotnet/sdk|dotnet/aspnet|supportdesk-api'
```

Expected results are `200` and a healthy/connected body from `/health`, `401` from `/api/tickets`
without a token, and `app` from `whoami`. The image-list command compares the local SDK,
ASP.NET runtime and application image sizes. To check restore caching, change a `.cs` file and
run `docker compose build --progress plain api`; the restore layer should be marked `CACHED`.

### Production Database Migration Strategy

Automatic migrations and demo seeding run only in Development (including this Compose setup).
Production startup does not run migrations. In production, run reviewed EF Core migrations in an
isolated, authorized pre-deployment release step using a database identity with schema-change
permissions. This avoids multiple application replicas racing to apply DDL and avoids granting
schema-migration privileges to the API's runtime database credentials. Seed demo accounts and
sample tickets are for local assessment use, not production data initialization.

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `slaStatus`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. Filters and count are applied server-side. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket with automatic priority, SLA, and agent triage |
| `POST /api/tickets/{id}/escalate` | Escalate a ticket and return the updated ticket plus its history row |
| `GET /api/tickets/{id}/escalations` | List a ticket's escalation history, newest first |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |
| `POST /api/auth/login` | Exchange an agent email and password for a JWT; anonymous |
| `GET /api/auth/me` | Return the signed-in agent from JWT claims |

All endpoints other than login require a valid bearer token. Swagger remains available in
Development. Errors are `ProblemDetails`: `400` validation, `401` missing/invalid credentials,
`404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

**SLA window decision:** The configured minimum window applies only after reducing a Premium
customer's base SLA window. Standard customers keep the configured base window unchanged; the
shortest configured base window is 4 hours.

**SLA filtering decision:** The SQL list filter duplicates the status boundaries from
`SlaEvaluator`, because EF cannot translate a call into the C# evaluator inside a database
`Where`. Keep the SQL expression and evaluator in sync; boundary tests cover their shared rules.

**Escalation design decisions:** Escalation history is a separate immutable entity rather than
a collection on `Ticket`, so growing history does not make the aggregate carry an unbounded
collection. `Ticket.Escalate` enforces the escalation guards, raises priority one level, applies
the due date and assignee computed by the caller, and returns the history row for persistence in
the same transaction. The `TicketId` index supports listing history for one ticket. Restricting
deletes through ticket and agent foreign keys prevents referenced history from being silently
removed. The reason is validated at the request boundary; the actor is taken from the validated
JWT. Domain business-rule violations map to HTTP 409. History lookup is newest-first using the deliberate
composite `(TicketId, EscalatedAtUtc)` index; EF also creates default indexes for the two agent
foreign keys, which are not query-specific indexes.

The escalation response is a wrapper containing both the updated ticket and its recorded
escalation row, rather than returning either object alone, so clients receive the refreshed ticket
state and the audit entry from the same operation.

## Authentication

Credentials are stored on `Agent` as an optional `PasswordHash`, not in a separate users table:
agents are the only principals in scope, and credentials share the agent's lifecycle. The
`AddAgentPasswordHash` EF migration adds the nullable column. Passwords use ASP.NET Core Identity's
`PasswordHasher<Agent>`. The Development seeder hashes a local fixture password at seed time; on
an already-seeded database it only fills missing hashes for known seed agents and never overwrites
an existing hash.

All active seeded agents use the obviously local-only password `Dev-Only-Password-1!`. For
example, sign in as `alex.turner@northwind-support.example`. The seeded Yuki Tanaka account is
inactive and cannot sign in. Do not reuse these fixture credentials outside local development.

JWTs expire after 60 minutes. Outside Development, provide a random `Jwt__Key` of at least 32
characters; the API fails at startup if it is missing or too short. The committed key in
`appsettings.Development.json` is fake and development-only. The web app stores the access token
and current-user data in `localStorage`, attaches the token as a bearer token to API calls, and
clears the session on any `401`. `localStorage` persists across reloads and tabs, but JavaScript on
the origin can read it, so an XSS vulnerability could expose the token. There are no refresh
tokens; an agent must sign in again after expiry.

Escalation requests contain only a reason. The API takes the actor email from the validated JWT,
so the caller cannot claim to have escalated as another agent.

## Known limitations

SLA risk after an escalation is evaluated using the original creation time and the current due
date (`due - created`) as the window. A ticket escalated several days after creation can therefore
show At risk immediately even when its recalculated due date is hours away. Measuring from the
most recent escalation would be more precise, but requires recording that timestamp to evaluate
the status and complicates the SQL SLA filter.

The SQL `AtRisk` and `WithinSla` filters use `DateDiffSecond`, while `SlaEvaluator` compares
exact ticks. A ticket within one second of the at-risk boundary can therefore be classified
differently by the list filter and the projected SLA status; this precision difference is accepted
for filtering.

Authentication does not include registration, password reset, email verification, OAuth/SSO,
refresh tokens, MFA, account lockout, or rate limiting. The access token is stored in
`localStorage` rather than an HttpOnly cookie, so preventing cross-site scripting remains
important.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
