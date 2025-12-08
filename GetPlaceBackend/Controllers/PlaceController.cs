using GetPlaceBackend.Dto.Block;
using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Dto.Reservation;
using GetPlaceBackend.Dto.UserAccess;
using GetPlaceBackend.Models;
using GetPlaceBackend.Services.Place;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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
    
    [HttpGet("card-place-list")]
    public async Task<IActionResult> GetCardPlaceList()
    {
        var result = await _placeService.GetPlacesByGroupIdAsync();
        return Ok(result);
    }

    [HttpGet("{placeShortId}/place-access")]
    public async Task<IActionResult> GetPlaceAccess([FromRoute] string placeShortId)
    {
        var result = await _placeService.GetPlaceAccessAsync(placeShortId);
        if (result == null)
            return NoContent();
        return Ok(result);
    }
    
    [HttpGet("{placeShortId}/place-user-access")]
    public async Task<IActionResult> GetPlaceUserAccess([FromRoute] string placeShortId)
    {
        var result = await _placeService.GetPlaceUserAccessAsync(placeShortId);
        return Ok(result);
    }
    
    [HttpGet("{placeShortId}/grids-and-reservations")]
    public async Task<IActionResult> GetGridsAndReservations([FromRoute] string placeShortId)
    {
        var result = await _placeService.GetGridsAndReservationsAsync(placeShortId);
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlaceCreateDto dto)
    {
        await _placeService.CreatePlaceAsync(dto);
        return Ok();
    }
    
    [HttpDelete("{placeShortId}")]
    public async Task<IActionResult> Delete(string placeShortId)
    {
        await _placeService.DeletePlaceAsync(placeShortId);
        return Ok();
    }
    
    [HttpPatch("name")]
    public async Task<IActionResult> UpdateName([FromBody] PlaceUpdateNameDto dto)
    {
        await _placeService.UpdatePlaceNameAsync(dto);
        return Ok();
    }
    
    [HttpPatch("access")]
    public async Task<IActionResult> UpdateAccess([FromBody] PlaceAccessUpdateDto dto)
    {
        await _placeService.UpdatePlaceAccessAsync(dto);
        return Ok();
    }
    
    [HttpPost("user-access")]
    public async Task<IActionResult> AddUserAccess([FromBody] UserAccessAddDto dto)
    {
        await _placeService.AddUserAccessAsync(dto);
        return Ok();
    }
    
    [HttpPost("block")]
    public async Task<IActionResult> AddBlock([FromBody] BlockCreateDto dto)
    {
        await _placeService.AddBlockAsync(dto);
        return Ok();
    }
    
    [HttpPatch("block-coordinates")]
    public async Task<IActionResult> UpdateBlockCoordinates([FromBody] BlockUpdateCoordinatesDto dto)
    {
        await _placeService.UpdateBlockCoordinatesAsync(dto);
        return Ok();
    }
    
    [HttpPatch("block-name")]
    public async Task<IActionResult> UpdateBlockName([FromBody] BlockUpdateNameDto dto)
    {
        await _placeService.UpdateBlockNameAsync(dto);
        return Ok();
    }
    
    [HttpDelete("{placeShortId}/block/{gridId}/{blockId}")]
    public async Task<IActionResult> DeleteBlock([FromRoute] string placeShortId, [FromRoute] string gridId, [FromRoute] string blockId)
    {
        var dto = new BlockDeleteDto(placeShortId, gridId, blockId);
        await _placeService.DeleteBlockAsync(dto);
        return Ok();
    }
    
    [HttpPost("reservation")]
    public async Task<IActionResult> AddReservation([FromBody] ReservationCreateDto dto)
    {
        await _placeService.AddReservationAsync(dto);
        return Ok();
    }
    
    [HttpDelete("{placeShortId}/reservation/{reservationId}")]
    public async Task<IActionResult> DeleteBlock([FromRoute] string placeShortId, [FromRoute] string reservationId)
    {
        var dto = new ReservationDeleteDto(placeShortId, reservationId);
        await _placeService.DeleteReservationAsync(dto);
        return Ok();
    }
}