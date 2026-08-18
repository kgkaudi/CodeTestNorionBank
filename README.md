# Gothenburg Congestion Tax Calculator

An ASP.NET Core 10 Web API that calculates the Gothenburg congestion tax
("trängselskatt") for a vehicle's toll station passages on a given day.

This started as a small code test: a working-but-rough shell (`TollCalculator`,
`Vehicle`, `Car`, `Motorbike`) with a couple of real bugs and no tests, API, or
extensibility. The brief was open-ended — think through what you'd change to
get code you "can stand by" — so this README documents both **what** was
built and **why**, as a record of that thinking.

## Contents

- [The problem](#the-problem)
- [Strategy](#strategy)
- [Architecture](#architecture)
- [Bugs found in the original shell, and how they were fixed](#bugs-found-in-the-original-shell-and-how-they-were-fixed)
- [Design decisions](#design-decisions)
- [Running it](#running-it)
- [Testing strategy](#testing-strategy)
- [Assumptions](#assumptions)
- [What I'd do next in a real project](#what-id-do-next-in-a-real-project)

## The problem

Each passage through a Gothenburg toll station costs 8, 13, or 18 SEK
depending on the time of day:

| Time          | Fee    |
|---------------|--------|
| 06:00–06:29   | 8 SEK  |
| 06:30–06:59   | 13 SEK |
| 07:00–07:59   | 18 SEK |
| 08:00–08:29   | 13 SEK |
| 08:30–14:59   | 8 SEK  |
| 15:00–15:29   | 13 SEK |
| 15:30–16:59   | 18 SEK |
| 17:00–17:59   | 13 SEK |
| 18:00–18:29   | 8 SEK  |
| 18:30–05:59   | 0 SEK  |

Rules on top of that table:

- **Daily cap**: max 60 SEK per vehicle per day.
- **60-minute rule**: a vehicle passing several toll stations within 60
  minutes of its first passage in that window is only charged once — the
  *highest* fee among those passages.
- **Toll-free days**: Saturdays, Sundays, public holidays, the day before a
  public holiday, and the whole month of July.
- **Toll-free vehicles**: motorbikes, tractors, emergency vehicles,
  diplomat vehicles, foreign-registered vehicles, and military vehicles.

## Strategy

Rather than patch the shell in place, the rebuild followed the natural
dependency order of the domain, bottom-up. This is the order the *code*
depends on itself — domain model, then the rules built on top of it, then
the API, then tests, then cleanup — rather than a literal claim about commit
order: the actual git history includes some early API/controller scaffolding
alongside the naming cleanup and merge-conflict resolution you'd expect from
working iteratively rather than planning the whole thing up front.

1. **Vehicle model** — `IVehicle`, `VehicleType`, one class per vehicle type,
   and a `VehicleFactory` to build one from a string. This is the most
   stable, lowest-level concept, so it made sense to get it right first and
   build everything else on top.
2. **Fee schedule** — the time → fee table, in isolation, independently
   testable against the table above with no date or vehicle logic mixed in.
3. **Holiday rules** — an `IPublicHolidayProvider` abstraction, because
   "which dates are toll-free" is really a calendar/config concern, not
   calculation logic, and it's the part most likely to need real data (or a
   different year) later.
4. **The calculator** — combines the three above, fixing the actual bugs in
   the original 60-minute/interval logic along the way (see below).
5. **API layer** — a thin controller + DTOs over the calculator, registered
   through DI in `Program.cs`. Kept deliberately thin: no business logic
   lives in the API project.
6. **Tests** — unit tests for the schedule, the holiday provider, and the
   calculator, then a second pass specifically targeting boundary conditions
   (see [Testing strategy](#testing-strategy)) so the bug classes below can't
   silently come back.
7. **Housekeeping** — removed the dead shell files (`TollCalculator.cs`,
   `Vehicle.cs` at the repo root) once the new code fully replaced them, and
   pinned a transitive NuGet package (`Microsoft.OpenApi`) to a patched
   version after `dotnet build` surfaced a `NU1903` high-severity advisory.

The domain project (`CodeSolution.Core`) has zero dependency on ASP.NET
Core. That's deliberate: the fee rules are pure business logic that should
be testable and reusable independent of how it's exposed (HTTP API today,
maybe a batch job or message handler tomorrow).

## Architecture

```
CodeSolution/
  CodeSolution.slnx
  src/
    CodeSolution.Core/            # domain logic — no framework dependency
      Vehicle/
        IVehicle.cs                # vehicle contract: a VehicleType
        VehicleType.cs              # Car, Motorbike, Tractor, Emergency, Diplomat, Foreign, Military
        Car.cs, Motorbike.cs, ...   # one small class per type
        VehicleFactory.cs           # string -> IVehicle, for the API layer
      Fees/
        TollFeeSchedule.cs          # time-of-day -> fee lookup
        TollFreeVehicles.cs         # which VehicleTypes are exempt
        ITollCalculator.cs
        TollCalculator.cs           # combines schedule + holidays + vehicle rules
      Holidays/
        IPublicHolidayProvider.cs
        SwedishPublicHolidayProvider.cs
    CodeSolution.Api/              # ASP.NET Core Web API — thin HTTP layer
      Controllers/TollFeeController.cs
      Contracts/TollFeeRequest.cs, TollFeeResponse.cs
      Program.cs
  tests/
    CodeSolution.Tests/
      Unit/TollFeeScheduleTests.cs
      Unit/PublicHolidayProviderTests.cs
      Unit/TollCalculatorTests.cs
  postman/
    NorionBankTest.postman_collection.json   # manual/API-level checks, see below
```

**Why split `Core` from `Api`:** the fee rules don't care whether they're
called from HTTP, a queue handler, or a console app. Keeping `Core`
framework-free means the unit tests run in milliseconds with no web host
spun up, and the rules can be reused elsewhere without dragging ASP.NET
Core along.

## Bugs found in the original shell, and how they were fixed

**1. The 60-minute window check compared the wrong thing.**
The original code did:
```csharp
long diffInMillies = date.Millisecond - intervalStart.Millisecond;
long minutes = diffInMillies / 1000 / 60;
```
`DateTime.Millisecond` is only the sub-second component (0–999) — it says
nothing about how many minutes apart two timestamps actually are. Two
passages an hour apart with the same millisecond value would show `minutes
== 0`. Fixed by comparing real elapsed time:
```csharp
(passage - windowStart).TotalMinutes <= ToleranceWindowMinutes
```

**2. An operator-precedence trap in the fee table.**
```csharp
else if (hour == 15 && minute >= 0 || hour == 16 && minute <= 59) return 18;
```
`&&` binds tighter than `||`, so this is actually
`(hour == 15 && minute >= 0) || (hour == 16 && minute <= 59)`. It happened
to produce the right fee here (because the 15:00–15:29 case was already
handled by an earlier branch), but it's exactly the kind of line that breaks
silently the next time someone touches it. Replaced with an explicit,
ordered list of time bands (`TollFeeSchedule`), each with its own start/end
— easy to read directly against the spec table, and there's no branch
ordering to get subtly wrong.

**3. Toll-exemption was a fragile string comparison.**
```csharp
String vehicleType = vehicle.GetVehicleType();
return vehicleType.Equals(TollFreeVehicles.Motorbike.ToString()) || ...
```
A typo in either the vehicle class's returned string or the enum name
breaks exemption silently — no compiler error, just a car that's suddenly
being charged (or a motorbike that isn't). Replaced with a `VehicleType`
enum on `IVehicle` and a single `TollFreeVehicles.IsTollFree(vehicle)`
lookup against a `HashSet<VehicleType>` — a typo here is now a compile
error, not a runtime surprise.

**4. "Day before a holiday" was hardcoded per date instead of derived.**
The original listed both a holiday and its eve as separate hardcoded dates
(e.g. both `3/28` and `3/29` for Good Friday), for a single year, all
crammed into one `if`. That means every eve day has to be remembered and
kept in sync by hand, for every year. Replaced with an
`IPublicHolidayProvider` that only lists actual holidays, plus a single,
generic rule applied once in `TollCalculator`:
```csharp
if (_holidayProvider.IsPublicHoliday(date.AddDays(1))) return true; // eve of a holiday
```
This also makes it obvious where a real multi-year Swedish holiday
calculation (or an external API) would plug in later.

## Design decisions

- **`VehicleType` enum + exemption lookup, not a `bool IsTollFree` on the
  vehicle itself.** Keeps the vehicle model pure data (what type is this?)
  and the tax-exemption rule (which types are exempt?) as a single,
  explicit, testable table in `Fees/`, rather than splitting a business rule
  across seven small classes.
- **Fee schedule as an ordered list of time bands**, not nested `if`/`else`.
  Matches the shape of the spec table directly, and a wrong band is a data
  error, not a logic error.
- **Sorting passages before grouping.** The original assumed `dates` arrived
  pre-sorted; the API can't guarantee that from a client, so
  `GetTollFee(vehicle, passages)` sorts defensively before applying the
  60-minute rule.
- **The 60-minute window is anchored to the passage that opened it, not
  re-anchored to the most recent passage.** Three passages 45 minutes apart
  each don't all group together — the third is 90 minutes from the one that
  opened the window, so it starts a new one. The spec doesn't say which
  interpretation is correct; this is the one implemented, and it's pinned
  down by a test so the behaviour is explicit rather than incidental.
- **DI-registered services (`TollFeeSchedule`, `IPublicHolidayProvider`,
  `ITollCalculator`)** rather than `static` classes, so the API project can
  swap in a different holiday provider (e.g. a real calendar API) purely
  through configuration, and so the calculator is trivially mockable in
  controller-level tests.

## Running it

```bash
cd CodeSolution
dotnet restore
dotnet build
dotnet test
dotnet run --project src/CodeSolution.Api
```

The API listens on `http://localhost:5238` — pinned explicitly in
`CodeSolution/src/CodeSolution.Api/Properties/launchSettings.json` (an HTTPS
profile is also available on `https://localhost:7231`). With `AddOpenApi()`,
the raw OpenAPI document is available at `/openapi/v1.json` in the
Development environment.

Example request:

**macOS / Linux / Git Bash:**
```bash
curl -X POST http://localhost:5238/api/tollfee/calculate \
  -H "Content-Type: application/json" \
  -d '{
        "vehicleType": "car",
        "passages": [
          "2013-01-02T06:00:00",
          "2013-01-02T07:30:00"
        ]
      }'
```

**Windows (PowerShell):**
```powershell
curl.exe -X POST http://localhost:5238/api/tollfee/calculate `
  -H "Content-Type: application/json" `
  -d '{"vehicleType": "car", "passages": ["2013-01-02T06:00:00", "2013-01-02T07:30:00"]}'
```
Note the explicit `curl.exe` — plain `curl` in PowerShell is aliased to
`Invoke-WebRequest`, which doesn't accept `-X`/`-d` the same way. Line
continuation also uses a backtick (`` ` ``) instead of a backslash.

```json
{ "vehicleType": "Car", "totalFee": 26, "currency": "SEK" }
```

You can find premade requests in the postman folder where there's a collection
with ready made calls for each case.

### Trying it in Postman

`CodeSolution/postman/NorionBankTest.postman_collection.json` has 16
requests grouped into four folders — Happy path, Toll-free days, Toll-free
vehicles, and Validation / error cases — each with a `pm.test` assertion on
status code and/or `totalFee`. Import the collection (the `baseUrl`
collection variable is already set to `http://localhost:5238`) and either
send requests individually or use **Run collection** to check all of them
at once. Like the `.http` file, this works identically regardless of OS.
This exercises the actual HTTP layer end-to-end (routing, model binding,
DTOs), which the unit tests below don't touch.

## Testing strategy

Tests are split to match the three responsibilities in `Core`, with each
file covering both the expected behaviour and the boundary conditions where
the original shell's bugs actually lived:

- **`TollFeeScheduleTests`** — every time band from the spec table at both
  its start and end minute, the toll-free overnight period, and midnight.
  A second set of tests checks the exact *second*-level boundary
  (`06:29:59` vs `06:30:00`) — the minute-level tests alone would still pass
  even if a band's end were accidentally shortened by a full minute.
- **`PublicHolidayProviderTests`** — every listed 2013 holiday, a regular
  day, and two negative cases that matter specifically because of how the
  "day before a holiday" rule was refactored: the eves of five holidays
  (proving the provider itself only knows about holidays, not their eves —
  that rule lives solely in `TollCalculator`), and a date outside 2013
  (documenting the provider's known single-year limitation, see
  [What I'd do next](#what-id-do-next-in-a-real-project)).
- **`TollCalculatorTests`** — toll-free vehicles (all six exempt
  `VehicleType`s, not just one), weekends, July, holidays, the
  day-before-a-holiday rule, unsorted input, the daily cap, and the
  60-minute grouping rule including its exact boundary (grouped at exactly
  60 minutes apart, charged separately at 61), the window-anchoring design
  decision above, and `ArgumentNullException` for a `null` vehicle or
  passages list.

**67 tests** currently pass across the two projects, plus the Postman
collection for API-level checks (see above).

## Assumptions

- All passages in a single `GetTollFee(vehicle, passages)` call belong to
  **one calendar day** — matching the original method's docstring
  ("date and time of all passes on one day"). The API doesn't currently
  validate this; a caller sending passages across midnight would get
  today's fee rules applied per-passage, but grouping/capping would still
  treat them as one set.
- The holiday list only covers 2013, matching the original test data — see
  [What I'd do next](#what-id-do-next-in-a-real-project).

## What I'd do next in a real project

- **Multi-year holiday data.** Several Swedish holidays are Easter-relative
  and move every year; `SwedishPublicHolidayProvider` would need a real
  calendar calculation or an external holiday service rather than a
  hardcoded 2013 list.
- **Fee schedule as configuration.** If the rate table changes periodically
  in practice, it belongs in `appsettings.json` (or a database) rather than
  a static array, so a rate change doesn't need a redeploy.