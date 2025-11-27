using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Services.Place;
using Microsoft.AspNetCore.Mvc;

namespace GetPlaceBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaceController : Controller
{
    private readonly IPlaceService _placeService;
    
    public PlaceController(IPlaceService placeService)
    {
        _placeService = placeService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlaceCreateDto dto)
    {
        await _placeService.CreatePlaceAsync(dto);
        return Ok();
    }
}