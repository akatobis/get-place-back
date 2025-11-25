using System.Collections.ObjectModel;
using GetPlaceBackend.Dto;
using GetPlaceBackend.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GetPlaceBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : Controller
{
    private static IMongoCollection<GroupModel> _collectionDb;
    
    public HomeController(IMongoDatabase db)
    {
        _collectionDb = db.GetCollection<GroupModel>("groups");
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var groups = await _collectionDb
            .Find(g => !g.IsDeleted)
            .SortBy(g => g.Order)
            .ToListAsync();

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

        var group = await _collectionDb
            .Find(g => g.GroupId == objectId && !g.IsDeleted)
            .FirstOrDefaultAsync();

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
        var maxOrder = await _collectionDb
            .Find(_ => true)
            .SortByDescending(g => g.Order)
            .Limit(1)
            .Project(g => g.Order)
            .FirstOrDefaultAsync();

        var newGroup = new GroupModel
        {
            Name = groupAddDto.Name,
            Order = maxOrder + 1
        };

        await _collectionDb.InsertOneAsync(newGroup);

        return Ok();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest("Invalid ID");

        var update = Builders<GroupModel>.Update
            .Set(g => g.IsDeleted, true);

        var result = await _collectionDb.UpdateOneAsync(
            g => g.GroupId == objectId,
            update
        );

        if (result.MatchedCount == 0)
            return NotFound("Group not found");

        return Ok("Group soft-deleted");
    }
    
    [HttpPatch("update-order")]
    public async Task<IActionResult> UpdateOrder([FromBody] GroupUpdateOrderDto dto)
    {
        if (!ObjectId.TryParse(dto.GroupId, out var objectId))
            return BadRequest(new { message = "Invalid ObjectId format" });

        var group = await _collectionDb
            .Find(g => g.GroupId == objectId && !g.IsDeleted)
            .FirstOrDefaultAsync();
        if (group == null)
            return NotFound(new { message = "Group not found" });

        await _collectionDb.UpdateManyAsync(
            g => g.Order >= dto.Order,
            Builders<GroupModel>.Update.Inc(g => g.Order, 1)
        );

        await _collectionDb.UpdateOneAsync(
            g => g.GroupId == objectId,
            Builders<GroupModel>.Update.Set(g => g.Order, dto.Order)
        );

        return Ok(new { message = "Order updated successfully" });
    }
    
    [HttpPatch("{id}/rename")]
    public async Task<IActionResult> Rename(string id, [FromBody] GroupRenameDto dto)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return BadRequest(new { message = "Invalid ObjectId format" });

        var group = await _collectionDb.Find(g => g.GroupId == objectId && !g.IsDeleted)
            .FirstOrDefaultAsync();

        if (group == null)
            return NotFound(new { message = "Group not found" });

        var update = Builders<GroupModel>.Update.Set(g => g.Name, dto.Name);

        await _collectionDb.UpdateOneAsync(g => g.GroupId == objectId, update);

        return Ok(new { message = "Group name updated successfully" });
    }

}