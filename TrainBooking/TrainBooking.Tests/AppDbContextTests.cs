using Microsoft.EntityFrameworkCore;
using TrainBooking.Api.Data;

namespace TrainBooking.Tests;

public class AppDbContextTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void SeedData_ShouldHaveTwoTrains()
    {
        using var ctx = CreateInMemoryContext();
        ctx.Database.EnsureCreated();
        Assert.Equal(2, ctx.Trains.Count());
    }

    [Fact]
    public void SeedData_EachTrainShouldHave20Seats()
    {
        using var ctx = CreateInMemoryContext();
        ctx.Database.EnsureCreated();
        var train = ctx.Trains.First();
        var seats = ctx.Seats.Where(s => s.TrainId == train.Id).ToList();
        Assert.Equal(20, seats.Count); // 2 coaches × 10 rows
    }
}
