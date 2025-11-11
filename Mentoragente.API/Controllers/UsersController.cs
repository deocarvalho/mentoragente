using Microsoft.AspNetCore.Mvc;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Application.Mappings;
using Mentoragente.Application.Validators;
using FluentValidation;

namespace Mentoragente.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Uncomment to require API key authentication
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IValidator<CreateUserRequestDto> _createValidator;
    private readonly IValidator<UpdateUserRequestDto> _updateValidator;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        IValidator<CreateUserRequestDto> createValidator,
        IValidator<UpdateUserRequestDto> updateValidator)
    {
        _userService = userService;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserListResponseDto>> GetUsers([FromQuery] PaginationRequestDto pagination)
    {
        var validator = new PaginationRequestValidator();
        var validationResult = await validator.ValidateAsync(pagination);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var result = await _userService.GetUsersAsync(pagination.Page, pagination.PageSize);
        
        var response = new UserListResponseDto
        {
            Users = result.Items.Select(u => u.ToDto()).ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        };

        return Ok(response);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        return Ok(user.ToDto());
    }

    /// <summary>
    /// Get user by phone number
    /// </summary>
    [HttpGet("phone/{phoneNumber}")]
    public async Task<ActionResult<UserResponseDto>> GetUserByPhone(string phoneNumber)
    {
        var user = await _userService.GetUserByPhoneAsync(phoneNumber);
        if (user == null)
        {
            return NotFound(new { message = $"User with phone number {phoneNumber} not found" });
        }

        return Ok(user.ToDto());
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserRequestDto request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var user = await _userService.CreateUserAsync(
            request.PhoneNumber,
            request.Name,
            request.Email);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user.ToDto());
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var user = await _userService.UpdateUserAsync(
            id,
            request.Name,
            request.Email,
            EntityToDtoMappings.ParseUserStatus(request.Status));

        return Ok(user.ToDto());
    }

    /// <summary>
    /// Delete user (soft delete - marks as Inactive)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        return NoContent();
    }
}
