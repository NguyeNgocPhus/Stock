using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;
using TrainBooking.Api.DTOs;

namespace TrainBooking.Api.Controllers;

[ApiController]
[Route("api/trains")]
public class TrainsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TrainsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trains = await _db.Trains
            .Select(t => new TrainDto
            {
                Id = t.Id, Name = t.Name, Origin = t.Origin,
                Destination = t.Destination,
                DepartureTime = t.DepartureTime, ArrivalTime = t.ArrivalTime
            })
            .ToListAsync();
        return Ok(trains);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var train = await _db.Trains
            .Where(t => t.Id == id)
            .Select(t => new TrainDto
            {
                Id = t.Id, Name = t.Name, Origin = t.Origin,
                Destination = t.Destination,
                DepartureTime = t.DepartureTime, ArrivalTime = t.ArrivalTime
            })
            .FirstOrDefaultAsync();

        if (train is null) return NotFound();
        return Ok(train);
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetAvailableSeats(int id)
    {
        var trainExists = await _db.Trains.AnyAsync(t => t.Id == id);
        if (!trainExists) return NotFound();

        var seats = await _db.Seats
            .Where(s => s.TrainId == id && !s.IsBooked)
            .Select(s => new SeatDto { Id = s.Id, Coach = s.Coach, Row = s.Row, Number = s.Number })
            .ToListAsync();

        return Ok(seats);
    }
}
