# SandKey.Api.Display

The backend for a touch-screen property kiosk in a Clearwater Beach real estate office. It reads
active MLS listings for postal code 33767 from the Bridge Data Output (Stellar MLS) feed, and
sends listing details to visitors who ask for them.

The kiosk itself has been running since 2009 — originally against an hourly CSV import, then as an
ASP.NET Core 6 MVC application consuming Bridge. This repository is the API half of the 2026
rebuild; the screen lives in `sandkey-display-web`.

## Endpoints

| | |
|---|---|
| `GET /api/display/listings?type=&condo=&sort=&order=&page=&pageSize=` | A page of listing summaries |
| `GET /api/display/listings/{listingKey}` | One listing in full |
| `GET /api/display/condos` | The condominium hotspots the home screen filters by |
| `POST /api/display/inquiries` | Send listing details to a visitor, or ask the office to call back |
| `GET /health` | Liveness: process only |
| `GET /ready` | Readiness: verifies the listing feed answers |

The OpenAPI document is served at `/openapi/v1.json` in development, generated from the XML
comments on the controllers and contract models.

## Running it

```bash
dotnet test
dotnet run --project SandKey.Api.Display
```

Two settings have no default and the application will not start without them, by design:

```bash
dotnet user-secrets set "Bridge:AccessToken" "<bridge token>" --project SandKey.Api.Display
dotnet user-secrets set "Mail:ApiKey" "<mailgun api key>" --project SandKey.Api.Display
```

Everything else has a working default in `appsettings.json`. In a deployed environment the two
secrets, and the blind-copy addresses, come from AWS Parameter Store under the path in
`Aws:ParameterStorePath`; `appsettings.json` holds defaults only and never a credential.

## Architecture

```
CloudFront ──/api/*──> Lambda (this API) ──> api.bridgedataoutput.com
                                        └──> api.mailgun.net
```

There is no cache. The kiosk serves one screen and the feed answers in a few hundred milliseconds,
so a cache would add an invalidation problem and a second failure mode to buy latency nobody is
waiting on. If that changes, `HybridCache` belongs in `ListingService`, behind `IListingService`.

Photos are not proxied. `MediaResponse.Url` is the Bridge media CDN URL as the feed supplies it, and
the kiosk loads it directly, so image bytes never pass through the application tier. The legacy
application proxied every photo through a controller. An earlier design fronted the CDN with a
`/media/*` behaviour on the kiosk's own distribution; it cannot work, because the Bridge CDN is
itself CloudFront and CloudFront refuses to act as an origin for another distribution.

## Standards

This repository follows the coding standard in `CLAUDE.md` (Parts 1–9). Three parts do not apply,
and there is one substitution. Each is a deliberate decision rather than an omission:

| Rule | Decision |
|---|---|
| **Part 1, Multi-Tenancy** | Not applicable. One brokerage, one kiosk, one dataset. There is no tenant to discriminate on, and adding a constant tenant column would be ceremony rather than isolation. |
| **Part 3, Identity and Access** | Not applicable. The kiosk is a screen in a window with no concept of a user, and every listing it shows is public IDX data. Rather than authenticate nobody, the one state-changing endpoint carries a fixed-window rate limit — without it, `POST /inquiries` would mail arbitrary addresses on demand. |
| **Part 5, Caching and Search** | Deliberately omitted. See Architecture above. Eighteen condominiums and a few hundred listings do not need an inverted index. |
| **Part 7, Telerik JustMock** | Substituted with NSubstitute. JustMock's commercial edition would make this repository unbuildable by anyone who clones it, including anyone evaluating it. Every other rule in Part 7 is followed: AAA comments, regions per method, `Create<Thing>()` fixtures, `Arrange_<Scenario>()` setups, `FakeLogger<T>` for log assertions, and an XML comment on every test method. |

Parts 2, 4, 6, 8 and 9 are followed in full. `TreatWarningsAsErrors`, `EnableNETAnalyzers` and
`EnforceCodeStyleInBuild` are on, so Parts 8 and 9 are enforced by the compiler rather than by
review: `dotnet build` and `dotnet format --verify-no-changes` both have to be clean.

### Folders

The layout is the one in Part 1. Four folders are additions under its "name the folder for the
noun it contains" rule: `Clients` (typed `HttpClient` wrappers, suffix `Client`), `Payloads` (the
Bridge wire contract, suffix `Payload`, kept separate from `Responses` which is our own contract),
`Handlers` (pipeline handlers, suffix `Handler`), and `Messages`/`Policies` for the outbound mail
message and the policy name constants.

Implementations are `internal`; the public surface is the interfaces in `Interfaces` plus the
request and response models. The test project sees the rest through `InternalsVisibleTo`.

## What changed from the legacy application

The port was an opportunity to fix things that were wrong rather than carry them across.

**The contract.** Four overloaded `Index` actions that encoded filters as path segments
(`/Residential/2/1/100`) became one endpoint filtered by query string. Three `GET` routes that
carried a visitor's email address or telephone number as path segments — logged by IIS on every
request — became one `POST` carrying them in the body. Collections come back in an envelope
reporting `totalCount`, so a caller can tell it has reached the end; the legacy kiosk could not,
which is why its Next button pages past the last result into an empty grid.

**The credential.** Bridge takes its access token as a query-string parameter. The legacy service
built it into the URI in the query factory and then wrote that URI to stdout on every call. Here a
delegating handler appends it to the outbound request, so every URI upstream of that one handler
is free of it and safe to log.

**Failure.** `catch (Exception) { return null; }` with a `//ToDo: log this` meant an upstream
outage was indistinguishable from "no listings match": the kiosk showed an empty grid either way.
Failures now throw typed exceptions that one `IExceptionHandler` maps to `ProblemDetails` — 502 for
an upstream error, 504 for a timeout, 404 for an unknown listing — and logs exactly once.

**Everything else.** `HttpClient` comes from `IHttpClientFactory` with the standard resilience
handler instead of being constructed and disposed per request. Responses are deserialized into
typed models by a source-generated `System.Text.Json` context rather than into `dynamic` via
Newtonsoft. Every I/O method takes a `CancellationToken` and forwards it. Configuration binds
through `IOptions<T>` with `ValidateDataAnnotations().ValidateOnStart()`, so a missing credential
stops the process at startup rather than failing on the first request that needs it.

**Bugs carried over from the old code, now fixed:** the collection mapper silently dropped six
fields the detail mapper kept (waterfront features, community features, pet policy, year built,
garage, and the modification timestamp); the rental email URL was built without a separating
slash, so rental enquiries had been broken; and a lease price was rendered with two currency
symbols. The three South Beach hotspots and the two Lighthouse Towers hotspots still map to one
subdivision each — that is intentional, and `CondoServiceTests` asserts it so a future edit has to
be deliberate about changing it.

## Deployment

Everything lives in one CloudFormation stack, `sandkey-display`, defined in `infra/template.yaml`
(AWS SAM). It creates the Lambda function and its function URL, the S3 bucket for the web bundle,
the certificate, the Route 53 alias, and one CloudFront distribution with two behaviours:

| Path | Origin |
|---|---|
| `/api/*` | The function URL, reachable only with CloudFront's signature (origin access control) |
| everything else | The S3 bucket, with client-side routes rewritten to `index.html` |

Because CloudFront signs requests to the function URL, a `POST` must carry the SHA-256 of its body
in `x-amz-content-sha256`. The web client computes it; a `GET` needs nothing.

```bash
infra/deploy.sh        # publish the API, deploy the stack, build and sync the web bundle
infra/deploy.sh api    # API and stack only
infra/deploy.sh web    # web bundle only
```

The stack reads its secrets from Parameter Store under `/production/sandkey-api`; the two that
must exist before the first deploy are `Bridge/AccessToken` and `Mail/ApiKey` as SecureStrings.
`Mail/BccAddresses/0`, `/1`, and so on are optional. Nothing in this repository or the stack holds
a credential.

## Tests

122 tests. The suite concentrates on the things that were historically untested and wrong: the
Bridge query shape against the contract recorded in `docs/bridge-contract.md`, the mapper
projections including the fields the old one dropped, every failure mode of the feed client,
the exception-to-status mapping, cancellation, options validation, and the rule that a visitor's
email address and telephone number never reach a log line.
