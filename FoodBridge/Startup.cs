﻿using FoodBridge.Data;
using FoodBridge.Middleware;
using FoodBridge.Services;
using Microsoft.EntityFrameworkCore;

namespace FoodBridge
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }
                );
            });

            // Configure database context based on environment
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
            {
                // Use in-memory database for testing
                services.AddDbContext<FoodDonationContext>(options =>
                    options.UseInMemoryDatabase("FoodDonationTestDb")
                );
            }
            else
            {
                // Use PostgreSQL for development
                services.AddDbContext<FoodDonationContext>(options =>
                    options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
                );
            }
            // Register repository
            services.AddScoped<IFoodItemRepository, FoodItemRepository>();
            services.AddSingleton<IAuthService, InMemoryAuthService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<FoodDonationContext>();
                    dbContext.Database.Migrate();
                    SeedData.Initialize(dbContext);
                }
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseRouting();
            app.UseMiddleware<AuthenticationMiddleware>();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
