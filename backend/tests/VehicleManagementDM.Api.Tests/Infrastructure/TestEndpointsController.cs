using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementDM.Api.Common.Auth;
using VehicleManagementDM.Api.Data;
using VehicleManagementDM.Api.Domain.Entities;
using VehicleManagementDM.Api.Domain.Enums;

namespace VehicleManagementDM.Api.Tests.Infrastructure;

/// <summary>Endpoints that exist only inside the test host, to provoke every kind of error.</summary>
[ApiController]
[Route("api/v1/test")]
public sealed class TestEndpointsController(AppDbContext db) : ControllerBase
{
    public const string SecretDetail = "Test failure with internal detail";

    [HttpGet("throw")]
    [AllowAnonymous]
    public IActionResult Throw() => throw new InvalidOperationException(SecretDetail);

    [HttpGet("schema-error")]
    [AllowAnonymous]
    public async Task<IActionResult> SchemaError()
    {
        await db.Database.ExecuteSqlRawAsync("SELECT * FROM [TableThatDoesNotExist]");
        return Ok();
    }

    [HttpPost("duplicate-login")]
    [AllowAnonymous]
    public async Task<IActionResult> DuplicateLogin()
    {
        var existing = await db.Users.AsNoTracking().FirstAsync();
        db.Users.Add(new User
        {
            LoginId = existing.LoginId,
            Role = UserRole.Admin,
            FullName = "Duplicate",
            Mobile = "+971500000003",
            Password = "x",
        });
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("not-found")]
    [AllowAnonymous]
    public IActionResult MissingRecord() => NotFound();

    [HttpPost("echo")]
    [AllowAnonymous]
    public IActionResult Echo(EchoRequest request) => Ok(request);

    [HttpGet("admin-only")]
    [Authorize(Roles = Roles.Admin)]
    public IActionResult AdminOnly() => Ok();

    public sealed class EchoRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; init; }

        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120.")]
        public int Age { get; init; }
    }
}
