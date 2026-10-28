# Partner Exchange Gateway

A small message gateway between a fictional national system and fictional coalition partners. It takes unit position and status reports, translates them to and from an invented format (PXF), stores them as events, and delivers them to partners that may be unreliable: retries, circuit breaking, dead letters, replay, recovery after a restart.

Everything here is made up. Nothing imitates or claims compliance with a real NATO standard, and there is no real defence data.

I'm building this to learn C#/.NET coming from Scala, and because the problems in it (format versions, lossy translation, delivery guarantees, partial failure) are ones I like.

## Status

`Work in progress`

Roadmap:

- [x] PXF format and translators
- [x] event-sourced domain
- [x] event store
- [x] gateway API
- [ ] outbound delivery + fault-injecting partner simulator
- [ ] retries, circuit breaker, dead letters
- [ ] signed inbound messages + idempotency
- [ ] restart recovery

## Running it

Unit tests:

`dotnet test` (needs the .NET 10 SDK).

Gateway: `dotnet run --project src\PartnerExchange.Gateway`

The gateway is running on port 5100

### API Documentation

- http://localhost:5100/scalar/v1

## Coming from Scala

Notes on where C# makes me change habits coming from Scala where I would use immutability and Either.

- Shared state: protecting concurrent reads and writes in memory with `lock`

  In the InMemoryEventStore several threads write to the same state, so it is guarded with `lock`. A next thread trying to access the same object will wait and then get the latest state.
  Within a lock you can't `await` inside a `lock`. If that part becomes async, for example when using a outside store like EventstoreDB I can switch to SemaphoreSlim.WaitAsync.

- Error handling: the `TryX` pattern instead of `Either`

  C# has no built-in Either. The idiomatic way is `bool TryParse(input, out var result)`,
  which avoids exceptions for expected failures and has the `out` variable in the signature instead of returning a tuple.
  The downside is that you lose the error detail unless you add a second `out`.
  For parsing that's fine, but I'm still deciding whether a small custom `Result<T>` type
  would be cleaner. It would be more what I'm used to but if the TryX pattern is more common I want to try and keep using that in stead.

## How I built this

I wrote the project outline. An implementation plan is created with AI assistance, including code snippets. I'm implementing milestone by milestone, writing every line and finding clearification on suggested C#/.NET patterns by consulting documentation and LLM, writing and running the tests at each step.
