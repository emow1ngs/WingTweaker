using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace KeyAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

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
            services.AddDbContext<KeyDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"), 
                    new MySqlServerVersion(new Version(8, 0, 21))));
            
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class KeyDbContext : DbContext
    {
        public KeyDbContext(DbContextOptions<KeyDbContext> options) : base(options) { }

        public DbSet<KeyInfo> Keys { get; set; }
        public DbSet<KeyType> KeyTypes { get; set; }
        public DbSet<SalesReport> Sales { get; set; }
    }

    [Table("keys")]
    public class KeyInfo
    {
        [Key]
        public int Id { get; set; }
        public string KeyValue { get; set; }
        public string MachineId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public string KeyType { get; set; }
        public string CustomerTelegram { get; set; }
        public double Price { get; set; }
    }

    [Table("key_types")]
    public class KeyType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int DurationDays { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    [Table("sales")]
    public class SalesReport
    {
        [Key]
        public int Id { get; set; }
        public int KeyId { get; set; }
        public DateTime SaleDate { get; set; }
        public double Amount { get; set; }
        public string CustomerTelegram { get; set; }
        public string Status { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class KeysController : ControllerBase
    {
        private readonly KeyDbContext _context;

        public KeysController(KeyDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            try
            {
                var keyInfo = new KeyInfo
                {
                    KeyValue = request.KeyValue,
                    MachineId = request.MachineId,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = request.ExpiryDate,
                    IsActive = true,
                    KeyType = request.KeyType,
                    CustomerTelegram = request.CustomerTelegram,
                    Price = request.Price
                };

                _context.Keys.Add(keyInfo);
                await _context.SaveChangesAsync();

                // Записываем продажу
                var sale = new SalesReport
                {
                    KeyId = keyInfo.Id,
                    SaleDate = DateTime.Now,
                    Amount = request.Price,
                    CustomerTelegram = request.CustomerTelegram,
                    Status = "Completed"
                };
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ключ создан успешно" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateKey(string key, string machineId)
        {
            try
            {
                var keyInfo = await _context.Keys
                    .FirstOrDefaultAsync(k => k.KeyValue == key && 
                                            k.MachineId == machineId && 
                                            k.IsActive && 
                                            k.ExpiryDate > DateTime.Now);

                return Ok(keyInfo != null);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetKeyInfo(string key)
        {
            try
            {
                var keyInfo = await _context.Keys
                    .FirstOrDefaultAsync(k => k.KeyValue == key);

                if (keyInfo == null)
                    return NotFound(new { success = false, message = "Ключ не найден" });

                return Ok(keyInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("deactivate")]
        public async Task<IActionResult> DeactivateKey(string key)
        {
            try
            {
                var keyInfo = await _context.Keys
                    .FirstOrDefaultAsync(k => k.KeyValue == key);

                if (keyInfo == null)
                    return NotFound(new { success = false, message = "Ключ не найден" });

                keyInfo.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ключ деактивирован" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserKeys(string telegram)
        {
            try
            {
                var keys = await _context.Keys
                    .Where(k => k.CustomerTelegram == telegram)
                    .OrderByDescending(k => k.CreatedDate)
                    .ToListAsync();

                return Ok(keys);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalKeys = await _context.Keys.CountAsync();
                var activeKeys = await _context.Keys.CountAsync(k => k.IsActive && k.ExpiryDate > DateTime.Now);
                var expiredKeys = await _context.Keys.CountAsync(k => k.ExpiryDate <= DateTime.Now);
                var totalRevenue = await _context.Sales.SumAsync(s => s.Amount);

                var keyTypeStats = await _context.Keys
                    .GroupBy(k => k.KeyType)
                    .Select(g => new
                    {
                        KeyType = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(k => k.Price)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalKeys = totalKeys,
                    ActiveKeys = activeKeys,
                    ExpiredKeys = expiredKeys,
                    TotalRevenue = totalRevenue,
                    KeyTypeStats = keyTypeStats
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class CreateKeyRequest
    {
        public string KeyValue { get; set; }
        public string MachineId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string KeyType { get; set; }
        public string CustomerTelegram { get; set; }
        public double Price { get; set; }
    }
}
