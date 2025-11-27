using GetPlaceBackend.Dto.Block;
using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Dto.UserAccess;
using MongoDB.Bson;

namespace GetPlaceBackend.Services.Place;

public interface IPlaceService
{
    public Task<List<CardPlaceDto>> GetPlacesByGroupIdAsync(ObjectId groupId);
    public Task<PlaceAccessDto?> GetPlaceAccessAsync(string placeShortId);
    public Task<PlaceUserAccessDto> GetPlaceUserAccessAsync(string placeShortId);
    public Task<PlaceGridsAndReservationsDto> GetGridsAndReservationsAsync(string placeShortId);
    public Task CreatePlaceAsync(PlaceCreateDto dto);
    public Task DeletePlaceAsync(string placeId);
    public Task UpdatePlaceAsync(PlaceUpdateNameDto dto);
    public Task AddUserAccessAsync(UserAccessAddDto dto);
    public Task AddBlockAsync(BlockCreateDto dto);
    public Task UpdateBlockCoordinatesAsync(BlockUpdateCoordinatesDto dto);
}