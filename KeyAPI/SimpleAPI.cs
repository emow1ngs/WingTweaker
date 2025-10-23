using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KeyAPI
{
    public class SimpleAPI
    {
        private static List<KeyInfo> keys = new List<KeyInfo>();
        private static string dataFile = "keys.json";

        public static void Main(string[] args)
        {
            LoadData();
            
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();
            
            app.UseCors("AllowAll");
            
            // API Endpoints
            app.MapPost("/api/keys/create", CreateKey);
            app.MapGet("/api/keys/validate", ValidateKey);
            app.MapGet("/api/keys/info", GetKeyInfo);
            app.MapPost("/api/keys/deactivate", DeactivateKey);
            app.MapGet("/api/keys/user", GetUserKeys);
            app.MapGet("/api/keys/stats", GetStats);
            
            app.Run();
        }

        private static async Task CreateKey(HttpContext context)
        {
            try
            {
                var request = await JsonSerializer.DeserializeAsync<CreateKeyRequest>(context.Request.Body);
                
                var keyInfo = new KeyInfo
                {
                    Id = keys.Count + 1,
                    KeyValue = request.KeyValue,
                    MachineId = request.MachineId,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = request.ExpiryDate,
                    IsActive = true,
                    KeyType = request.KeyType,
                    CustomerTelegram = request.CustomerTelegram,
                    Price = request.Price
                };

                keys.Add(keyInfo);
                await SaveData();

                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = true, message = "Ключ создан успешно" }));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        private static async Task ValidateKey(HttpContext context)
        {
            try
            {
                string key = context.Request.Query["key"];
                string machineId = context.Request.Query["machineId"];

                var keyInfo = keys.Find(k => k.KeyValue == key && 
                                          k.MachineId == machineId && 
                                          k.IsActive && 
                                          k.ExpiryDate > DateTime.Now);

                await context.Response.WriteAsync((keyInfo != null).ToString().ToLower());
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        private static async Task GetKeyInfo(HttpContext context)
        {
            try
            {
                string key = context.Request.Query["key"];
                var keyInfo = keys.Find(k => k.KeyValue == key);

                if (keyInfo == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = "Ключ не найден" }));
                    return;
                }

                await context.Response.WriteAsync(JsonSerializer.Serialize(keyInfo));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        private static async Task DeactivateKey(HttpContext context)
        {
            try
            {
                string key = context.Request.Query["key"];
                var keyInfo = keys.Find(k => k.KeyValue == key);

                if (keyInfo == null)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = "Ключ не найден" }));
                    return;
                }

                keyInfo.IsActive = false;
                await SaveData();

                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = true, message = "Ключ деактивирован" }));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        private static async Task GetUserKeys(HttpContext context)
        {
            try
            {
                string telegram = context.Request.Query["telegram"];
                var userKeys = keys.FindAll(k => k.CustomerTelegram == telegram);
                
                await context.Response.WriteAsync(JsonSerializer.Serialize(userKeys));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        private static async Task GetStats(HttpContext context)
        {
            try
            {
                var stats = new
                {
                    TotalKeys = keys.Count,
                    ActiveKeys = keys.Count(k => k.IsActive && k.ExpiryDate > DateTime.Now),
                    ExpiredKeys = keys.Count(k => k.ExpiryDate <= DateTime.Now),
                    TotalRevenue = keys.Sum(k => k.Price),
                    KeyTypeStats = keys.GroupBy(k => k.KeyType)
                        .Select(g => new { KeyType = g.Key, Count = g.Count(), Revenue = g.Sum(k => k.Price) })
                        .ToList()
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(stats));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message = ex.Message }));
            }
        }

        private static void LoadData()
        {
            try
            {
                if (File.Exists(dataFile))
                {
                    string json = File.ReadAllText(dataFile);
                    keys = JsonSerializer.Deserialize<List<KeyInfo>>(json) ?? new List<KeyInfo>();
                }
            }
            catch
            {
                keys = new List<KeyInfo>();
            }
        }

        private static async Task SaveData()
        {
            try
            {
                string json = JsonSerializer.Serialize(keys, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dataFile, json);
            }
            catch
            {
                // Игнорируем ошибки сохранения
            }
        }
    }

    public class KeyInfo
    {
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
