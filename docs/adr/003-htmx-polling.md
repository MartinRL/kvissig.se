# ADR 003: HTMX with Polling for Real-Time Updates

## Status
Accepted

## Context
Players need to see when others have answered and when the game advances. Options:
1. SignalR (WebSockets)
2. Server-Sent Events (SSE)
3. HTMX polling
4. Blazor Server

## Decision
Use **HTMX with 2-second polling** for all real-time updates.

```html
<div hx-get="/game/ABC123/state" 
     hx-trigger="every 2s"
     hx-swap="innerHTML">
  <!-- Server returns current game state as HTML -->
</div>
```

## Rationale
- **Same-room gaming**: 2-second latency is imperceptible when players are physically together
- **Simplicity**: No WebSocket connection management, reconnection logic, or SignalR infrastructure
- **Debuggability**: All state changes visible in browser Network tab as plain HTTP
- **Stateless server**: No connection state, easy horizontal scaling
- **CDN/proxy friendly**: Works through any infrastructure
- **Resilience**: Missed poll = try again in 2 seconds, no broken state

## Load Analysis
- 100 users × 1 request/2 seconds = 50 requests/second
- Each request: ~1KB response (HTML fragment)
- Trivial load for any server

## Consequences
- Maximum 2-second delay for state updates (acceptable)
- Slightly more bandwidth than WebSockets (acceptable for hobby project)
- Server renders HTML for every poll (cheap, enables server-side logic)
- No need for sticky sessions or backplane
- PWA/service worker compatible
