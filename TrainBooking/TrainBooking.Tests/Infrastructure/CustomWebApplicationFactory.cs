using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TrainBooking.Api.Data;

namespace TrainBooking.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Pre-compute the DB name at factory construction time
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations to avoid dual-provider conflict.
            // EF Core registers: DbContextOptions<T>, DbContextOptions, AppDbContext,
            // IDbContextOptionsConfiguration<T>, ServiceProviderAccessor
            var typesToRemove = new HashSet<Type>
            {
                typeof(DbContextOptions<AppDbContext>),
                typeof(DbContextOptions),
                typeof(AppDbContext),
                typeof(ServiceProviderAccessor),
                typeof(IDbContextOptionsConfiguration<AppDbContext>),
            };

            var descriptorsToRemove = services
                .Where(d => typesToRemove.Contains(d.ServiceType))
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Add InMemory database using the pre-computed name
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        builder.UseEnvironment("Development");
    }

    // Called each time CreateClient() is invoked — seed once on first client creation
    protected override void ConfigureClient(System.Net.Http.HttpClient client)
    {
        if (!_seeded)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            _seeded = true;
        }

        base.ConfigureClient(client);
    }
}
