# Replace ILogger with Serilog Static Logger Design

**Date:** 2026-03-22
**Project:** TrainBooking.Api (.NET 9)

## Goal

Replace the injected `ILogger<BookingService>` in `BookingService` with Serilog's static API (`Log.Information`, `Log.Warning`).

## Changes

### `BookingService.cs`
- Remove `private readonly ILogger<BookingService> _logger;` field
- Remove `ILogger<BookingService> logger` constructor parameter and `_logger = logger;` assignment
- Replace `_logger.LogInformation(...)` → `Log.Information(...)`
- Replace `_logger.LogWarning(...)` → `Log.Warning(...)`
- Remove `using Microsoft.Extensions.Logging;`, add `using Serilog;`

### `BookingServiceTests.cs`
- Remove `NullLogger<BookingService>.Instance` from all 6 `BookingService` constructor calls
- Remove `using Microsoft.Extensions.Logging.Abstractions;`

### `Program.cs`
- No changes — `Log` static is already initialized via `builder.Host.UseSerilog(...)`

## Out of Scope
- No other files touched
- No changes to logging configuration
