using DailyOneRosterFile.Api.BackgroundServices;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using DailyOneRosterFile.Api.Services;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<FileVariantOptions>(builder.Configuration.GetSection(FileVariantOptions.SectionName));
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<IOneRosterFileGenerator, OneRosterFileGenerator>();
builder.Services.AddScoped<IOneRosterValidator, OneRosterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHostedService<DailyFileGenerationWorker>();
builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("FrontendDev");
}

app.UseDefaultFiles();
app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
