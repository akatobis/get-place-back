using GetPlaceBackend.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace GetPlaceBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet("{tgId}")]
    public async Task<IActionResult> AddReservation([FromRoute] string tgId)
    {
        var result = await _userService.GetById(tgId);
        if (result == null)
            return NoContent();
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddReservation([FromQuery] string tgId, [FromQuery] string username)
    {
        await _userService.CreateOrUpdate(tgId, username);
        return Ok();
    }
}