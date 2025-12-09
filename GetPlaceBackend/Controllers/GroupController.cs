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
    public async Task<IActionResult> Get([FromQuery] string userId)
    {
        var groups = await _service.GetAll(userId);

        return Ok(groups.Select(g => new GroupGetDto
        {
            GroupId = g.GroupId.ToString(),
            Name = g.Name,
            Order = g.Order
        }));
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        var group = await _service.GetByIdAsync(id);

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
        var result = await _service.AddAsync(groupAddDto.Name, groupAddDto.UserId);
        return Ok(result);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        var result = await _service.SoftDeleteAsync(id);

        return result 
            ? Ok("Group soft-deleted") 
            : NotFound("Group not found");
    }
    
    [HttpPatch("update-order")]
    public async Task<IActionResult> UpdateOrder([FromBody] GroupUpdateOrderDto dto)
    {
        var result = await _service.UpdateOrderAsync(dto.GroupId, dto.Order);
        
        return result 
            ? Ok(new { message = "Order updated successfully" })
            : NotFound(new { message = "Group not found" });
    }
    
    [HttpPatch("{id}/rename")]
    public async Task<IActionResult> Rename([FromRoute] string id, [FromBody] GroupRenameDto dto)
    {
        var result = await _service.RenameAsync(id, dto.Name);

        return result 
            ? Ok(new { message = "Group name updated successfully" }) 
            : NotFound(new { message = "Group not found" });
    }

}