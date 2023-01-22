using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MinAPI.Data;
using MinAPI.Dtos;
using MinAPI.Models;

var builder = WebApplication.CreateBuilder(args);
var sqlConnBuilder = new SqlConnectionStringBuilder();
var Cors = "Cors";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: Cors,
        builder =>
        {
            builder.WithOrigins("*");
            builder.AllowAnyMethod();
            builder.AllowAnyHeader();
        });
});

sqlConnBuilder.ConnectionString = builder.Configuration.GetConnectionString("SQLDbConnection");
sqlConnBuilder.UserID = builder.Configuration["UserId"];
sqlConnBuilder.Password = builder.Configuration["Password"];

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(sqlConnBuilder.ConnectionString));
builder.Services.AddScoped<ICommandRepo, CommandRepo>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

app.UseCors(Cors);

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("api/v1/commands", async (ICommandRepo _repo, IMapper _mapper) => {
    var commands = await _repo.GetAllCommands();
    return Results.Ok(_mapper.Map<IEnumerable<CommandReadDto>>(commands));
});

app.MapGet("api/v1/commands/{id:int}", async (ICommandRepo _repo, IMapper _mapper, int id) => {
    var command = await _repo.GetCommandById(id);
    if (command is not null) return Results.Ok(_mapper.Map<CommandReadDto>(command));
    return Results.NotFound();
});

app.MapPost("api/v1/commands", async (ICommandRepo _repo, IMapper _mapper, CommandCreateDto cmdCreateDto) => {
    var command = _mapper.Map<Command>(cmdCreateDto);
    await _repo.CreateCommand(command);
    await _repo.SaveChangesAsync();
    var cmdReadDto = _mapper.Map<CommandReadDto>(command);
    return Results.Created($"api/v1/commands/{cmdReadDto.Id}", cmdReadDto);
});

app.MapPut("api/v1/commands/{id:int}", async (ICommandRepo _repo, IMapper _mapper, int id, CommandUpdateDto cmdUpdateDto) => {
    var command = await _repo.GetCommandById(id);
    if (command is null) return Results.NotFound();
    _mapper.Map(cmdUpdateDto, command);
    await _repo.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("api/v1/commands/{id:int}", async (ICommandRepo _repo, IMapper _mapper, int id) => {
    var command = await _repo.GetCommandById(id);
    if (command is null) return Results.NotFound();
    _repo.DeleteCommand(command);
    await _repo.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();