# Partner Exchange Gateway

### Project outline
A message gateway that translates and delivers reports between a fictional national system and fictional coalition partners.
The gateway will accept information (unit position and status updates) in a standardised invented format, parse this and store this in events and a projection. Information is then shared to connected partners. Information can go both ways.
The gateway will survive an unreliable partner system: it will have durable outbox, retries, per-partner circuit breaking, dead-lettering and replay, and recovery after restart.

This is fictional: nothing imitates or claims compliance with any real NATO standard, no real defence data, classification markings, or secrets are used.

This is a demo project to learn and practice C#/.NET coming from Scala.

> Work in progress

### Roadmap:
- [ ] setup of invented PXF format and its translators
- [ ] event-sourced domain
- [ ] event-store
- [ ] gateway API
- [ ] outbound delivery + fault-injecting partner simulator
- [ ] fault tolerance (retries, circuit breaker, dead letters)
- [ ] signed inbound + idempotency
- [ ] restart recovery

### How I built this
I wrote this project outline and created the implementation plan with AI assistance, including code snippets and examples. I wrote every line of code myself, and made sure I understood each component before moving on.