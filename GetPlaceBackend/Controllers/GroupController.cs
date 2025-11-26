using System.Collections.ObjectModel;
using GetPlaceBackend.Dto;
using GetPlaceBackend.Models;
using GetPlaceBackend.Services.Group;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GetPlaceBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupController : Controller
{
    private readonly IGroupService _service;
    
    public GroupController(IGroupService service)
    {
        _service = service;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get(string userId)
    {
        if (!ObjectId.TryParse(userId, out var userObjectId))
            return BadRequest(new { message = "Invalid ObjectId format" });
        
        var groups = await _service.GetAll(userObjectId);

        return Ok(groups.Select(g => new GroupGetDto
        {
            GroupId = g.GroupId.ToString(),
            Name = g.Name,
            Order = g.Order
        }));
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest(new { message = "Invalid ObjectId format" });

        var group = await _service.GetByIdAsync(objectId);

        if (group == null)
            return NotFound(new { message = "Group not found" });

        var resultDto = new GroupGetDto
        {
            GroupId = group.GroupId.ToString(),
            Name = group.Name,
            Order = group.Order
        };

        return Ok(resultDto);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] GroupAddDto groupAddDto)
    {
        if (!ObjectId.TryParse(groupAddDto.UserId, out var userId))
            return BadRequest(new { message = "Invalid ObjectId format" });
        
        await _service.AddAsync(groupAddDto.Name, userId);
        return Ok();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest("Invalid ID");

        var result = await _service.SoftDeleteAsync(objectId);

        return result 
            ? Ok("Group soft-deleted") 
            : NotFound("Group not found");
    }
    
    [HttpPatch("update-order")]
    public async Task<IActionResult> UpdateOrder([FromBody] GroupUpdateOrderDto dto)
    {
        if (!ObjectId.TryParse(dto.GroupId, out var objectId))
            return BadRequest(new { message = "Invalid ObjectId format" });

        var result = await _service.UpdateOrderAsync(objectId, dto.Order);
        
        return result 
            ? Ok(new { message = "Order updated successfully" })
            : NotFound(new { message = "Group not found" });
    }
    
    [HttpPatch("{id}/rename")]
    public async Task<IActionResult> Rename(string id, [FromBody] GroupRenameDto dto)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest(new { message = "Invalid ObjectId format" });
        
        var result = await _service.RenameAsync(objectId, dto.Name);

        return result 
            ? Ok(new { message = "Group name updated successfully" }) 
            : NotFound(new { message = "Group not found" });
    }

}