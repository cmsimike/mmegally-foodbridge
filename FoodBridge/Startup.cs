using FoodBridge.Data;
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

            // Add EF Core with in-memory database
            services.AddDbContext<FoodDonationContext>(options =>
                options.UseInMemoryDatabase("FoodDonationDb")
            );

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

                // Seed the in-memory database
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FoodDonationContext>();
                    SeedData.Initialize(context);
                }
            }

            app.UseHttpsRedirection();
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
