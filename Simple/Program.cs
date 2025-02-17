using System.Reflection;
using Attrilith.Service;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(); // 添加 MVC 服务

builder.Services.AddSmartServices(
    new AutoRegisterOptions
    {
        AutoRegisterByConvention = false,
        AutoRegisterByAttribute = true,
        AutoRegisterHostedServices = true,
    },
    Assembly.GetExecutingAssembly()
);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "觅心阁测试API",
            Version = "v1",
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();