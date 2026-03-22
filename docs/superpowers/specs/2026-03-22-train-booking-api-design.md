# Train Ticket Booking API — Design Spec

**Date:** 2026-03-22
**Stack:** .NET 9, ASP.NET Core Web API, EF Core 9, PostgreSQL

---

## Overview

A simple REST API for booking train tickets. Passengers can browse available trains, view available seats, and create a booking. No authentication required (public API). Cancellation is out of scope for this version.

---

## Project Structure

```
TrainBooking/
├── TrainBooking.Api/
│   ├── Controllers/
│   │   ├── TrainsController.cs
│   │   └── BookingsController.cs
│   ├── Models/
│   │   ├── Train.cs
│   │   ├── Seat.cs
│   │   └── Booking.cs
│   ├── DTOs/
│   │   ├── BookingRequest.cs
│   │   └── BookingResponse.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Services/
│   │   └── BookingService.cs
│   └── Program.cs
```

---

## Technology Stack

- **Framework:** ASP.NET Core Web API (.NET 9)
- **ORM:** Entity Framework Core 9
- **Database:** PostgreSQL (via Npgsql provider)
- **API Docs:** Swagger / OpenAPI
- **Validation:** Data Annotations on DTOs

---

## Data Models

### Train
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK, auto-increment |
| Name | string | e.g. "Express 101" |
| Origin | string | Departure city |
| Destination | string | Arrival city |
| DepartureTime | DateTime | UTC |
| ArrivalTime | DateTime | UTC |

### Seat
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK, auto-increment |
| TrainId | int | FK → Train |
| Coach | string | e.g. "A", "B" |
| Row | int | e.g. 1–20 |
| Number | string | e.g. "1A", "1B" |
| IsBooked | bool | Default: false |

### Booking
| Field | Type | Notes |
|-------|------|-------|
| Id | int | PK, auto-increment |
| BookingReference | string | Unique. Auto-generated: "TRN-" + 6 random alphanumeric chars |
| TrainId | int | FK → Train |
| SeatId | int | FK → Seat |
| PassengerName | string | Required |
| PassengerEmail | string | Required, valid email format |
| BookedAt | DateTime | UTC, set on creation |

---

## Data Seeding

Trains and seats are pre-seeded via EF Core's `HasData()` in `AppDbContext`. There is no admin endpoint for creating trains or seats — they are managed directly in the seed data. Each train is seeded with a fixed set of seats (e.g., coaches A–B, rows 1–10, giving 20 seats per coach).

---

## API Endpoints

### GET /api/trains
List all trains.

**Response 200:**
```json
[
  {
    "id": 1,
    "name": "Express 101",
    "origin": "Hanoi",
    "destination": "Ho Chi Minh City",
    "departureTime": "2026-03-25T08:00:00Z",
    "arrivalTime": "2026-03-25T20:00:00Z"
  }
]
```

---

### GET /api/trains/{id}
Get a single train's details.

**Response 200:** Same shape as a single item from `GET /api/trains`.

**Response 404:** Train not found.

---

### GET /api/trains/{id}/seats
List available (unbooked) seats for a specific train.

**Response 200:**
```json
[
  { "id": 5, "coach": "A", "row": 1, "number": "1A" }
]
```

**Response 404:** Train not found.

---

### POST /api/bookings
Create a new booking.

**Request:**
```json
{
  "trainId": 1,
  "seatId": 5,
  "passengerName": "John Doe",
  "passengerEmail": "john@example.com"
}
```

**Response 201:**
```json
{
  "bookingReference": "TRN-A1B2C3",
  "trainName": "Express 101",
  "seat": "A-1A",
  "passengerName": "John Doe",
  "passengerEmail": "john@example.com",
  "bookedAt": "2026-03-22T10:00:00Z"
}
```

**Error responses:**
- `400` — Missing/invalid fields
- `404` — Train or seat not found
- `409` — Seat already booked or seat does not belong to the specified train

---

### GET /api/bookings/{reference}
Get booking details by booking reference.

**Response 200:**
```json
{
  "bookingReference": "TRN-A1B2C3",
  "trainName": "Express 101",
  "seat": "A-1A",
  "passengerName": "John Doe",
  "passengerEmail": "john@example.com",
  "bookedAt": "2026-03-22T10:00:00Z"
}
```

**Response 404:** Booking not found.

---

## Business Logic (`BookingService`)

1. Validate that `trainId` exists → `404` if not
2. Validate that `seatId` exists AND `seat.TrainId == trainId` → `409` if mismatch
3. Begin a PostgreSQL serializable transaction
4. Re-check `seat.IsBooked` inside the transaction → `409` if already booked
5. Generate booking reference: `TRN-` + 6 random uppercase alphanumeric chars; retry up to 5 times on collision (collision is exceedingly rare at this scale but must not be silently ignored)
6. Set `seat.IsBooked = true`, create `Booking` record, commit transaction
7. Return `BookingResponse` DTO

**Concurrency strategy:** Use a serializable isolation level transaction in EF Core (`IsolationLevel.Serializable`) to prevent double-booking under concurrent requests. This is sufficient for the expected load of a simple booking system.

---

## Error Response Body

All error responses follow ASP.NET Core's built-in `ProblemDetails` format (RFC 7807):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "PassengerEmail is required."
}
```

Custom error messages are set in the `detail` field.

---

## Error Handling Summary

| Scenario | HTTP Status |
|----------|-------------|
| Invalid/missing request fields | 400 Bad Request |
| Train or seat not found | 404 Not Found |
| Seat already booked | 409 Conflict |
| Seat does not belong to train | 409 Conflict |
| Booking reference not found | 404 Not Found |
| Success (create) | 201 Created |
| Success (read) | 200 OK |

---

## Out of Scope

- Authentication / authorization
- Booking cancellation or modification
- Payment processing
- Admin endpoints for creating/managing trains and seats
