# Partner Exchange Gateway

A small message gateway between a fictional national system and fictional coalition partners. It takes unit position and status reports, translates them to and from an invented format (PXF), stores them as events, and delivers them to partners that may be unreliable: retries, circuit breaking, dead letters, replay, recovery after a restart.

Everything here is made up. Nothing imitates or claims compliance with a real NATO standard, and there is no real defence data.

I'm building this to learn C#/.NET coming from Scala, and because the problems in it (format versions, lossy translation, delivery guarantees, partial failure) are ones I like.

## Status

Work in progress:

Roadmap:

- [x] PXF format and translators
- [ ] event-sourced domain
- [ ] event store
- [ ] gateway API
- [ ] outbound delivery + fault-injecting partner simulator
- [ ] retries, circuit breaker, dead letters
- [ ] signed inbound messages + idempotency
- [ ] restart recovery

## Running it

`dotnet test` (needs the .NET 10 SDK).

## Coming from Scala

- using lock to prevent threads writing to concurrently to shared component state
- The TryX pattern that is used, in stead of the Either monad, and declaring the out var in the method signature, instead of returning a tuple

## How I built this

I wrote the project outline. An implementation plan is created with AI assistance, including code snippets. I'm implementing milestone by milestone, writing every line and finding clearification on suggested C#/.NET patterns by consulting documentation and LLM, and running the tests at each step. Where I deviate from the plan, the commit message says so.
