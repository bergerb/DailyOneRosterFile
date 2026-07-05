# Worker Startup + Midnight Cron Scheduling Design

## Goal
Run daily OneRoster generation once at app startup and then on a strict midnight schedule each night.

## Scope
- `DailyFileGenerationWorker` execution timing behavior
- Cron-based recurrence using local server/container time
- Preservation of existing one-file retention behavior

## Decisions
- Use a cron-based schedule in the current `BackgroundService`.
- Use Cronos package (not Quartz/Hangfire).
- Cron expression: `0 0 * * *` (daily at 00:00 local time).

## Architecture
Keep the existing `BackgroundService` and add a two-phase schedule:
1. Run generation + retention immediately on service startup.
2. Enter cron-driven loop that waits until the next local midnight occurrence, then runs generation + retention again.

Cron occurrence calculation is recomputed each cycle so the schedule remains aligned to midnight across normal clock drift and DST transitions.

## Data Flow
1. Worker starts.
2. Worker triggers generation.
3. Worker applies retention cleanup (keep latest ZIP only).
4. Worker calculates next cron occurrence from current local time.
5. Worker delays until next occurrence.
6. Worker repeats steps 2-5 until cancellation.

## Error Handling
- If startup run fails, log the error and continue into scheduled loop.
- If scheduled run fails, log the error and continue to next scheduled occurrence.
- Cancellation token interrupts wait and stops worker gracefully.
- Retention cleanup continues to run only as part of generation flow.

## Testing Strategy
- Test startup behavior: one generation attempt occurs before first schedule wait.
- Test scheduling behavior: next occurrence is computed from cron expression `0 0 * * *`.
- Test near-midnight timing behavior to confirm next execution targets the next midnight boundary.
- Keep existing retention behavior verification unchanged.

## Success Criteria
- App startup triggers one immediate generation attempt.
- Subsequent generation attempts align to local midnight schedule.
- Worker remains resilient across failures and continues scheduling.
