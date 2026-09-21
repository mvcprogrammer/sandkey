# C# and WebAPI Coding Standards

## Preface

This document is the product of two years of work documenting coding and API standards, on my own time and equipment, while working at an enterprise-level SaaS organization. I took the initiative of defining how engineers and coding assistants can collaborate on disparate codebases. This document was conceived while writing notes for Copilot instructions and grew into a personal engineering standard. Every rule here has been tested against live pull requests, code reviews, and shipped production code.

## How to use this document

The document is organized from the decisions that are hardest to reverse to cheapest to fix. Architecture and project structure come first because they shape everything downstream. Formatting comes last because a tool can enforce it. Within each part, sections are ordered by impact, and within a section, items of equal weight are listed alphabetically.

This copy is trimmed to SandKey.Api.Display: a single-tenant, unauthenticated, cache-free API on AWS Lambda that reads one MLS feed and sends mail. Parts 3 and 5 do not apply and are kept only as numbered placeholders so references elsewhere in the repository stay valid; the reasons are recorded in the README.

The document is written to be consumed by engineers first and AI assistants second. Rules are stated as imperatives, examples are concrete, and terms are used consistently so that an assistant can apply them without interpretation.

| Part | Scope                              | Why it comes where it does                                               |
|------|------------------------------------|--------------------------------------------------------------------------|
| 1    | Architecture and Project Structure | Changes here ripple through every file                                   |
| 2    | Design Practices                   | Shapes runtime behavior, resilience, and observability                   |
| 3    | Identity and Access                | Not applicable to this project; see README                               |
| 4    | WebAPI Standards                   | Defines the public contract consumers depend on                          |
| 5    | Data Stores: Caching and Search    | Not applicable to this project; see README                               |
| 6    | Cloud-Native Operations            | Determines whether the service survives contact with production          |
| 7    | Testing Standards                  | Guards every change that follows                                         |
| 8    | Naming                             | Affects readability of every identifier, cheap to fix per-instance       |
| 9    | Formatting                         | Fully tool-enforced; lowest cost of error                                |

---

# Part 1: Architecture and Project Structure

**Applies to:** all engineers.

## Project and Solution Layout
* One project per deployable unit or reusable library. Tests live in a sibling `{ProjectName}.Test` project.
* Project names follow `{Org}.{ProjectType}.{ProjectName}[.{OptionalSubComponent}]` (for example `SandKey.Api.Display`). The repository is named for the primary project; its test project lives alongside it.
* Enable `TreatWarningsAsErrors` and the `Microsoft.CodeAnalysis.NetAnalyzers` analyzers in every project.
* Enable nullable reference types (`<Nullable>enable</Nullable>`) in every project.
* Keep a `.editorconfig` at the solution root that encodes the tool-enforceable rules in Parts 8 and 9 (formatting, naming rules, and analyzer severities) so `dotnet format` and the analyzers enforce them. A matching `.editorconfig` accompanies this document.
* Pin package versions and use `Directory.Packages.props` (central package management) for multi-project solutions.

## Folder Structure
Organize each project by the kind of object it contains. Folder names are plural nouns; classes inside carry a matching prefix or suffix; namespaces follow the folder.

| Folder         | Contents                                                                                     | Naming convention                                                                             |
|----------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| Clients        | Typed `HttpClient` wrappers around an upstream service.                                      | Suffix `Client`                                                                               |
| Configurations | Options objects used to configure the library's services.                                    | Suffix `Options`                                                                              |
| Controllers    | MVC controllers that accept input, map it to the model, and return results.                  | Suffix `Controller`                                                                           |
| Enumerations   | Enumerations used by the library.                                                            | No suffix                                                                                     |
| Exceptions     | Custom exceptions raised by the library.                                                     | Suffix `Exception`                                                                            |
| Extensions     | Extension methods that extend objects or interfaces.                                         | Suffix `Extensions`                                                                           |
| Factories      | Factories that create an object from a set of inputs.                                        | Suffix `Factory`                                                                              |
| Handlers       | `DelegatingHandler` and `IExceptionHandler` implementations in the request pipelines.        | Suffix `Handler`                                                                              |
| Interfaces     | Interfaces used for Inversion of Control in the DI container.                                | Prefix `I`                                                                                    |
| Messages       | Outbound messages (the mail message sent to a visitor).                                      | Suffix `Message`                                                                              |
| Payloads       | The upstream wire contract as deserialized from Bridge; distinct from Responses.             | Suffix `Payload`                                                                              |
| Policies       | Named policy constants (rate limiting, resilience).                                          | Suffix `Policy`                                                                               |
| Requests       | API request models and DTOs accepted by controllers (Part 4).                                | Suffix `Request`                                                                              |
| Responses      | API response models and DTOs returned by controllers, including the paged envelope (Part 4). | Suffix `Response`                                                                             |
| Services       | Business logic between controllers and clients.                                              | Suffix `Service`                                                                              |

This list is not exhaustive. When an object does not fit an existing category, create a folder named for the noun it represents and apply the same conventions. Keep folder depth shallow; nested folders should not be necessary.

## Namespaces
* Namespaces match the full project name and folder structure, delimited by periods: `{Org}.{ProjectType}.{ProjectName}.{FolderName}` (for example `SandKey.Api.Display.Controllers`).
* Use file-scoped namespace declarations. Do not wrap the file contents in a namespace block.

## Files and Objects
* Each file contains exactly one type.
* File names match the name of the type they contain, including casing.
* Type names describe what the type contains and carry the prefix or suffix required by their folder.

## Dependency Injection
* Depend on interfaces, not concrete types. Every injectable service has an interface in the Interfaces folder.
* Register services in extension methods on `IServiceCollection` (`AddXxxServices()`) in the Extensions folder.
* Bind configuration through the Options pattern (`IOptions<T>` or `IOptionsMonitor<T>`) using classes from the Configurations folder. Do not read `IConfiguration` directly in services.
* Choose lifetimes deliberately: `Singleton` for stateless, thread-safe services; `Scoped` for per-request state; `Transient` for lightweight, stateful objects.
* Never inject a scoped service into a singleton.

---

# Part 2: Design Practices

**Applies to:** all engineers.

## Exception Handling
* Design classes so that exceptions can be avoided in the normal path.
* Throw exceptions rather than returning error codes.
* Catch the most specific exception type available. Do not catch `System.Exception` unless the handler is a top-level boundary.
* Use predefined .NET exception types. Create a custom exception only when no existing type applies.
* When defining a custom exception, expose additional properties so consumers can determine what went wrong without parsing the message.
* Do not log-and-rethrow. Log an exception at the layer that handles it; if a layer only propagates, it neither catches nor logs. Unhandled exceptions are logged once by the exception-handling middleware (Part 4). When rethrowing to add context, wrap in a new exception with the original as `InnerException`.

## Async and Cancellation
* Any method that performs I/O is `async` and returns `Task` or `Task<T>`. Never use `async void` outside event handlers.
* Async method names end with `Async`.
* Every async method accepts a `CancellationToken` as its last parameter and forwards it to all awaited calls.
* Never block on async code with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
* Do not use `ConfigureAwait(false)`. It is unnecessary in ASP.NET Core application code.

## Resilience
* Create every outbound `HttpClient` through `IHttpClientFactory`. Never instantiate `HttpClient` directly.
* Apply `AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience` to every named or typed client. It provides rate limiting, total request timeout, retry with jitter, circuit breaker, and per-attempt timeout with recommended defaults.
* Every external call has an explicit timeout. Never rely on library defaults.
* Retry only operations that are idempotent or protected by an idempotency key. Never retry a non-idempotent POST blindly.
* Define fallback behavior for each dependency: what the service does when the dependency is down, and how that state is surfaced in health checks.

## Logging
* Inject typed loggers (`ILogger<T>`) through the constructor. Child classes receive their logger through the parent constructor.
* Use structured logging with named placeholders: `_logger.LogInformation("Processed {Count} items", count)`. Never use string interpolation or concatenation in a log call.
* Use `LoggerMessage` source-generated methods for high-frequency log calls.
* Pass the exception as the first argument when logging it (`_logger.LogError(ex, "...")`) so the stack trace is captured.
* Choose levels deliberately: `Trace` and `Debug` for diagnostics, `Information` for normal milestones, `Warning` for recoverable problems, `Error` for failure of the current operation, `Critical` for failures that take down the process.
* Never log PII, client-confidential material, secrets, or connection strings.

## Nullability and Immutability
* Treat nullable warnings as errors.
* Validate constructor and public method arguments up front with `ArgumentNullException.ThrowIfNull()` and related guard helpers.
* Mark fields `readonly` when they are assigned only in the constructor.
* Prefer `record` types for DTOs and value-like objects.
* Expose `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, or `IEnumerable<T>` on public surfaces rather than mutable collection types.

## Enumerations
* Specify an explicit starting value for every enumeration. Assign a value to each member when the values are persisted or exchanged across a boundary.
* Validate incoming values before use: `Enum.IsDefined` for plain enumerations; for `[Flags]` enumerations, mask the value against the union of all defined members and reject any remaining bits. `Enum.HasFlag` tests a bit; it does not validate.

## Generics
* Omit the type argument at a call site when the compiler can infer it, and readability does not suffer.

## HTTP Requests
* Use lowercase URIs when calling another service over HTTP.

## Strings and Disposable Resources
* Use string interpolation (`$"..."`) rather than `string.Format` or concatenation.
* Use `StringBuilder` when building a string in a loop.
* Specify `StringComparison.Ordinal` or `OrdinalIgnoreCase` explicitly for non-linguistic comparisons.
* Wrap `IDisposable` resources in a `using` declaration or statement.

## Variables
* Declare variables as close as possible to their first use.
* Use `var` when the type is clear from the right-hand side of the assignment.

---

# Part 3: Identity and Access

Not applicable. The kiosk has no users and serves public IDX data. The one state-changing endpoint is protected by a fixed-window rate limit instead; see the README.

---

# Part 4: WebAPI Standards

**Applies to:** backend and full-stack engineers; API consumers should read Rules for Endpoints, HTTP Verbs, and Response Codes.

## Overview
A WebAPI is an application that exposes its Application Programming Interface over a network using HTTP. It is a concept, not a technology. This document implements APIs with Microsoft ASP.NET Core and follows Representational State Transfer (REST) conventions. In the examples below, `display` is the application segment of the route (from the project name `SandKey.Api.Display`), not a resource.

## Rules for Endpoints
* NEVER include client-confidential material (unreleased images, contracts, payment details) or Personally Identifiable Information (PII) in a URL.
* An endpoint identifies a resource, never an action. Do not append an action after the resource.
* Resource names are plural.
* Resource names are lowercase and contain only letters of the alphabet.
* An endpoint does not embed other resources in its path.
* Filtering is expressed through query string parameters.
* The API version is not part of the endpoint path.

When strict adherence would produce a URL that misrepresents the resource, favor a clear, resource-oriented name and document the exception in the controller's XML comments.

## HTTP Verbs
| Verb    | Use                                                                                                                                                                          | Notes                                                                                                                                                                                                                                                                 |
|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| GET     | Fetch a single resource (`GET /api/display/listings/5`) or a collection (`GET /api/display/listings?type=residential`).                                                         | Idempotent. Must not modify state. Repeated calls return the same result for the same state. GET requests never carry a body.                                                                                                                                         |
| POST    | Create a resource (`POST /api/display/listings`). Also used to start asynchronous processes, and as a search verb when the criteria contain PII or other confidential values. | Not idempotent by default; a create without an idempotency key produces a new resource on every call (see Resilience, Part 2). Return `201 Created` with a `Location` header for a create, `202 Accepted` when starting asynchronous work, and `200 OK` for a search. |
| PUT     | Replace a single resource (`PUT /api/display/listings/5`) or a collection (`PUT /api/display/listings`).                                                                       | Idempotent. The caller sends a complete, modified version of a previously fetched resource; omitted fields are treated as cleared.                                                                                                                                    |
| PATCH   | Partially update a resource (`PATCH /api/display/listings/5`) or a range (`PATCH /api/display/listings?type=residential`).                                                      | Only the supplied fields change. Prefer JSON Merge Patch (RFC 7386) or JSON Patch (RFC 6902) bodies.                                                                                                                                                                  |
| DELETE  | Delete a resource (`DELETE /api/display/listings/5`) or a range (`DELETE /api/display/listings?type=residential`).                                                              | Idempotent. May be a soft delete that sets a display flag rather than removing the row. Deleting an already-deleted resource returns `204` or `404` consistently, never an error.                                                                                     |
| HEAD    | Return the headers a GET would return, without the body (`HEAD /api/display/listings/5`).                                                                                     | Rarely needed in application code.                                                                                                                                                                                                                                    |
| OPTIONS | Return the communication options for a resource (`OPTIONS /api/display/listings`).                                                                                            | Used by browsers for CORS preflight; ASP.NET's CORS middleware handles it. Can also serve as a permission check without a real GET.                                                                                                                                   |

## Response Codes
Return the most specific status code that describes the outcome. A consumer should be able to act on the status code alone without parsing the body.

### Success (2xx)
| Code | Name       | When to use                                                                                                                                  |
|------|------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| 200  | OK         | A GET, PUT, PATCH, or POST-as-search completed and the body contains the resource or result set.                                             |
| 201  | Created    | A POST created a resource. Include a `Location` header and return the created resource in the body.                                          |
| 202  | Accepted   | The request was accepted for asynchronous processing. Return a status resource the caller can poll.                                          |
| 204  | No Content | The request succeeded and there is nothing to return. Typical for DELETE, and for PUT or PATCH when the updated resource is not echoed back. |

### Client errors (4xx)
| Code | Name                   | When to use                                                                                                                 |
|------|------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| 400  | Bad Request            | The request is malformed or fails model validation. Return a `ValidationProblemDetails` body describing each invalid field. |
| 404  | Not Found              | The resource does not exist.                                                                                                |
| 405  | Method Not Allowed     | The verb is not supported for this endpoint. ASP.NET returns this automatically.                                            |
| 409  | Conflict               | The request conflicts with the current state: duplicate create, concurrency or ETag mismatch, invalid state transition.     |
| 415  | Unsupported Media Type | The request `Content-Type` is not accepted by the endpoint.                                                                 |
| 422  | Unprocessable Content  | The request is well-formed but violates a business rule. Use 400 unless the distinction matters to consumers.               |
| 429  | Too Many Requests      | The caller exceeded a rate limit. Include a `Retry-After` header.                                                           |

### Server errors (5xx)
| Code | Name                  | When to use                                                                                                                                 |
|------|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| 500  | Internal Server Error | An unhandled exception occurred. Never return exception details; log them and return a generic `ProblemDetails` body with a correlation ID. |
| 502  | Bad Gateway           | An upstream service returned an invalid response.                                                                                           |
| 503  | Service Unavailable   | The service is temporarily unable to handle requests. Include a `Retry-After` header when possible.                                         |
| 504  | Gateway Timeout       | An upstream service did not respond in time.                                                                                                |

### Rules
* Never include PII or client-confidential material in error messages or `ProblemDetails` bodies.
* Use RFC 9457 (formerly RFC 7807) `ProblemDetails` and `ValidationProblemDetails` as the body for every 4xx and 5xx response. Do not invent a custom error envelope.
* Do not return 200 with an error payload. The status code is the contract.
* Do not return 500 for expected conditions such as not-found, validation failure, or conflict. Reserve 5xx for genuine faults.
* Declare every status code an endpoint can return with `[ProducesResponseType]` so the OpenAPI document is accurate.
* Let unhandled exceptions propagate to exception-handling middleware (`UseExceptionHandler` or `IExceptionHandler`), which logs them and translates them into `ProblemDetails`. The middleware is the single place an unhandled exception is logged; lower layers catch and log only when they add context or handle the exception, and they do not log-and-rethrow (see Exception Handling, Part 2).
* Map custom exceptions to status codes in the exception handler, not in individual controllers.

## Controllers
* Keep controllers thin: validate input, call a service, map the result to a response. Business logic belongs in services.
* Apply `[ApiController]` to every API controller for automatic model validation and `ProblemDetails` responses.
* Use attribute routing (`[Route("api/[controller]")]`, `[HttpGet("{id}")]`) rather than convention-based routing.
* Return `IActionResult` or `ActionResult<T>` from every action, using the `ControllerBase` helper methods to produce status codes.
* Annotate every endpoint with the appropriate `[ProducesResponseType]` and `[Produces]` attributes.
* Declare every I/O-bound action `async` with a `CancellationToken` as its last parameter, forwarded to all downstream calls.
* Inject dependencies through the constructor. Do not use service location or static access.
* Do not catch exceptions in a controller to return their details to the caller.
* Version the API through the `Accept` header or a query string parameter, never the URL path.

## Request and Response Models
* Never expose an upstream payload (Payloads folder) directly from an endpoint. Map to a dedicated request or response model (Requests and Responses folders, Part 1).
* Make request and response models immutable where practical (`record` types or `init` setters).
* Validate request models with data annotations or FluentValidation. Do not hand-roll validation inside actions.
* Serialize with `System.Text.Json` using camelCase property names.
* Return collections in an envelope rather than an unbounded array: `items`, `totalCount`, `page`, `pageSize`.

## OpenAPI-Compatible XML Comments
* Enable `GenerateDocumentationFile` in every API project and wire the output into the OpenAPI generator: the built-in `Microsoft.AspNetCore.OpenApi` XML comment support (the default from .NET 10), Swashbuckle's `IncludeXmlComments`, or the NSwag equivalent.
* Use `<summary>` for the consumer-facing description of every controller, action, and request or response model. This text becomes the OpenAPI `description` field, so write it for the API consumer, not for the next engineer reading the source.
* Document every action parameter with `<param>` and its return value with `<returns>`.
* Use `<response code="...">` to describe each status code declared via `[ProducesResponseType]`, and keep the two in sync. A declared status code with no matching `<response>` tag is a defect.
* Reserve `<remarks>` for implementation detail that should not appear in the public contract.
* When a property, type, or shape is reused across multiple request or response models (a paged envelope, a shared value object, a common enum), document it once on its canonical, most-frequently-referenced definition. Every other occurrence uses `<inheritdoc cref="..."/>` rather than restating the description, so the OpenAPI schema's `description` fields cannot drift out of sync when the canonical definition changes.

---

# Part 5: Data Stores: Caching and Search

Not applicable. There is no cache and no search index; the feed is read directly on every request. If a cache is ever added it belongs in `ListingService` behind `IListingService` using `HybridCache`; see the README.

---

# Part 6: Cloud-Native Operations

**Applies to:** all engineers.

## Configuration and Secrets
* Configuration comes from environment variables and a secrets manager (AWS Secrets Manager or Parameter Store). `appsettings.json` contains defaults only, never secrets, or environment-specific values.
* Bind configuration through the Options pattern (Part 1). Validate options at startup with `ValidateDataAnnotations()` and `ValidateOnStart()` so a misconfigured service fails fast.
* Use IAM roles for AWS access. Never embed access keys in configuration or code.

## Health and Lifecycle
* Every service exposes a liveness endpoint (`/health`) that reports process health only, and a readiness endpoint (`/ready`) that verifies dependencies.
* Register dependency checks through `Microsoft.Extensions.Diagnostics.HealthChecks`. A failing dependency degrades readiness, not liveness, unless the service cannot function without it.
* Handle `SIGTERM` gracefully: stop accepting requests, drain in-flight work, then exit. Configure the host shutdown timeout to exceed the longest expected request.
* The service is stateless. Nothing is held in process memory across requests; a Lambda instance may be recycled at any time.

## Observability
* Instrument with OpenTelemetry (`OpenTelemetry.Extensions.Hosting`) for traces, metrics, and logs. Export to the platform's collector; do not couple application code to a vendor SDK.
* Generate a correlation ID at the edge and propagate it through HTTP headers (W3C `traceparent`). Every log line includes it.
* Emit RED metrics (rate, errors, duration) per endpoint and per outbound dependency.
* Logs are structured JSON to stdout. The platform, not the application, handles shipping and retention.
* Define alerts on symptoms (error rate, latency) rather than causes.

## Deployment
* Every artifact is built once and promoted through environments unchanged. Environment differences are configuration only.
* Feature flags gate behavior changes, so deploy and release are separate decisions.
* Infrastructure is defined in code and reviewed through the same process as application code.

---

# Part 7: Testing Standards

**Applies to:** all engineers; QA and SDET roles in full.

## Framework and Libraries
* Use xUnit as the test framework for all .NET projects.
* Use NSubstitute for mocks and dependency substitution. Implementations are `internal`, so the API project exposes them to the test project and to `DynamicProxyGenAssembly2` through `InternalsVisibleTo`.
* Use `FakeLogger<T>` (namespace `Microsoft.Extensions.Logging.Testing`, shipped in the `Microsoft.Extensions.Diagnostics.Testing` package) to verify logging. The same package supplies the fake time provider and metrics collectors used below.
* Use `coverlet.collector` for code coverage.
* Prefer Microsoft's official testing utilities over third-party alternatives where both exist.

## Coverage Requirements
* Test every public method and constructor.
* Test both the happy path and every exception path.
* Test edge cases: null inputs, empty collections, invalid parameters.
* Test cancellation-token handling and timeout scenarios.
* Test partial-failure scenarios where some operations succeed before a failure occurs.
* Verify logging in both success and failure scenarios.
* Verify configuration usage and dependency interaction.
* Verify data serialization and transformation logic.
* Verify interface implementations and type assignments.

## Structure and Organization
* Follow the Arrange-Act-Assert pattern in every test, with `// Arrange`, `// Act`, and `// Assert` comments marking each section.
* Group related tests in `#region` blocks (`#region Constructor Tests`, `#region SendMessageAsync Tests`). Test classes follow this layout instead of the member-kind regions in Part 9.
* Order regions as constructor tests, then one region per method under test, then a `#region Helper Methods` at the bottom of the class.
* Document every test method with an XML comment stating the behavior being validated.

## Independence and Maintenance
* Every test runs in isolation and does not depend on the order or outcome of other tests.
* Do not share mutable test data between tests. Each test builds its own fixtures through helper methods.
* Keep each test focused on a single responsibility.
* Use consistent naming patterns for mock variables and test data.
* Update mock configurations when a dependency's interface changes.
* Review test data periodically so it continues to reflect current business requirements.

## Test Data and Mocking
* Create helper methods with descriptive names for test data and common mock setups. Factories that build fixtures are named `Create<Thing>()` (`CreateTestClients()`, `CreateMockOptions()`); methods that configure a scenario across several mocks are named `Arrange_<Scenario>()` (see Data-Driven Tests).
* Use realistic test data and include both valid and invalid data sets.
* Mock only the dependencies of the class under test; never mock the class itself.
* Create substitutes with `Substitute.For<T>()`, arrange with `.Returns(...)` or `.Throws(...)`, and verify interactions with `.Received(n)` or `.DidNotReceive()` when the interaction itself is the behavior under test.
* Match arguments explicitly. Use `Arg.Any<T>()` only when the value is irrelevant to the test.
* Use explicit parameter matching rather than relying on implicit overload resolution.
* Use `Arg.Do<T>()` to capture method parameters.
* Format multi-parameter mock arrangements with each parameter on its own line, indented four spaces.
* Comment complex mock setups.

## Assertions
* Use `Assert.Equal()` for exact value comparisons, including exception messages.
* Use `Assert.Contains()` for partial string matching in serialized data.
* Use `Assert.True()` and `Assert.False()` for boolean conditions, with a descriptive comment.
* Use `Assert.NotNull()` to verify object creation.
* Use `Assert.IsType<T>()` and `Assert.IsAssignableFrom<T>()` for type verification.
* Use `Assert.Single()` when exactly one element is expected, and use its return value as the element under assertion.
* Include a descriptive failure message where it aids diagnosis.

## Async Testing
* Test every async method with an async test method.
* Arrange `Task<T>`-returning methods with `.Returns(value)`; NSubstitute wraps the value. A `Task`-returning method needs no arrangement to complete.
* Use `Assert.ThrowsAsync<TException>()` for async exceptions.
* Pass `CancellationToken.None` in every call that is not testing cancellation.
* Test cancellation with both a pre-canceled token and a token canceled mid-operation.

## Exception Testing
* Verify the specific exception type and message content.
* Verify that an exception is logged at the layer that handles it (a service that catches and recovers, or the exception-handling middleware). Do not require log-and-rethrow in lower layers; see Response Code Rules, Part 4.

## Logging Verification
* Retrieve log entries with `FakeLogger<T>.Collector.GetSnapshot()`.
* Verify level, message, and exception on the captured records. Use `Assert.Single()` on the snapshot when exactly one entry is expected.
* Verify that expected entries appear at the correct level, covering both `Information` and `Error` scenarios.
* Do not attempt to verify logging scopes through `StructuredState`. Focus on message content and level.

## Data-Driven Tests
* Use `[Theory]` with `[InlineData]` for small literal cases and `[MemberData]` or `[ClassData]` for cases that need objects or shared sets.
* Name the test method for the behavior under test as in Part 8; the data attributes describe the variation, not the name.
* Name scenario-setup methods `Arrange_<ScenarioDescription>()` and call them from the `// Arrange` section.

---

# Part 8: Naming

**Applies to:** all engineers.

## Casing
Casing follows the Microsoft Framework Design Guidelines. The accompanying `.editorconfig` enforces every rule below as a build error.

* Constants, namespaces, types, interfaces, methods, properties, events, enums, and enum members: `PascalCase`.
* Parameters and locals: `camelCase`.
* Private and internal fields: `_` prefix followed by `camelCase`. Public fields are not used; expose a property instead.
* Test method names are the one exception to `PascalCase` methods: they use the underscore-delimited patterns in the Unit Tests below. The `.editorconfig` relaxes the method naming rule and CA1707 (identifiers should not contain underscores) for `*Tests.cs` files only.
* Hungarian notation is not used.

## Extension Methods
* The containing static class is named for the extended type with an `Extensions` suffix. Extension methods for `IServiceCollection` live in `ServiceCollectionExtensions`.

## Unit Tests
* Test project: the project under test with a `.Test` suffix (singular).
* Test class: the class under test with a `Tests` suffix (plural). The asymmetry is intentional: one project, many tests per class.
* Constructor tests: the name of the class under test.
* Method tests: the method under test followed by `_Should<ExpectedResult>`, where the expected result is in PascalCase and states what the test proves.
* Overloaded methods: include the parameter types in the name to distinguish the overload.
* Behavior-specific suffixes: `ShouldLog` for logging, `ShouldUse` for configuration, `ShouldSerialize` or `ShouldTransform` for data transformation, `ShouldThrow<ExceptionType>` for cancellation and failure, `ShouldInclude` for scope validation in partial-failure tests, and `ShouldComplete` or `ShouldThrow` for empty or null input.

---

# Part 9: Formatting

**Applies to:** all engineers. The Braces, New Lines, Indentation, and Whitespace rules are enforced by the accompanying `.editorconfig` through `dotnet format`. The Layout and Comments rules are enforced in code review.

## Braces
* Allman style: every opening and closing brace is on its own line, aligned with the statement that owns it.
* Braces are always used, including for single-line statements.

```csharp
public class Example
{
    public void DoWork(int count)
    {
        if (count > 0)
        {
            Process(count);
        }
        else
        {
            Skip();
        }

        try
        {
            Run();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Run failed; using cached result");
            UseCachedResult();
        }
        finally
        {
            Cleanup();
        }
    }
}
```

## New Lines
* Open and close braces go on their own line for all constructs: types, methods, local functions, properties, indexers, events, accessors, anonymous methods, anonymous types, control blocks, lambda expressions, and object, collection, array, and `with` initializers.
* `catch`, `else`, and `finally` each begin a new line.
* Members in anonymous types begin a new line.
* Members in object initializers begin a new line.
* Query expression clauses begin a new line.

## Indentation
* Use soft tabs of four spaces. Never use tab characters.
* Indent block contents.
* Indent case labels.
* Indent case contents, except when the contents are a block.
* Do not indent the close brace.
* Labels are indented one level less than the current level.
* The first continuation line is indented one level more than the line it continues; later continuation lines match the first.

## Whitespace
* Insert a single space before and after binary operators.
* Insert a space after a comma.
* Insert a space after keywords in control-flow statements.
* Insert a space after the semicolon in a `for` statement.
* Insert a space before and after the colon in a base or interface declaration.
* No space after a cast.
* No space after a dot.
* No space before a comma.
* No space before a dot.
* No space before a square bracket.
* No space before the semicolon in a `for` statement.
* No space between a method name and its opening parenthesis, in both declarations and invocations.
* No space in declaration statements.
* No space within parentheses of control-flow statements, expressions, or type casts.
* No space within parameter-list parentheses, empty or otherwise.
* No space within square brackets, empty or otherwise.

## Layout
* Group members into `#region` blocks by kind, in this order: constants, fields, constructors, properties, events, interface implementations, public methods, private methods. Test classes use the region layout in Part 7 instead.

## Comments and Documentation
* Add XML documentation to every type and to every member except private fields and private constants. The compiler (CS1591 under `GenerateDocumentationFile` and `TreatWarningsAsErrors`) enforces this for public and protected members; code review enforces it for internal and private members.
* Explain magic values with a comment on the same line or the line above.
* For controllers and API request/response models, XML comments follow the OpenAPI-specific rules in Part 4 (tag usage and deduplication via `<inheritdoc>`); this section's rule applies as-is to everything else.
