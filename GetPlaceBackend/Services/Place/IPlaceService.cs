using GetPlaceBackend.Dto.Block;
using GetPlaceBackend.Dto.Place;
using GetPlaceBackend.Dto.Reservation;
using GetPlaceBackend.Dto.UserAccess;
using GetPlaceBackend.Models;
using MongoDB.Bson;

namespace GetPlaceBackend.Services.Place;

public interface IPlaceService
{
    public Task<List<PlaceModel>> GetPlacesByGroupIdAsync(string? userId = null);
    public Task<PlaceAccessDto?> GetPlaceAccessAsync(string placeShortId);
    public Task<PlaceUserAccessDto> GetPlaceUserAccessAsync(string placeShortId);
    public Task<PlaceGridsAndReservationsDto> GetGridsAndReservationsAsync(string placeShortId);
    public Task<string> CreatePlaceAsync(PlaceCreateDto dto);
    public Task DeletePlaceAsync(string placeId);
    public Task UpdatePlaceNameAsync(PlaceUpdateNameDto dto);
    public Task UpdatePlaceAccessAsync(PlaceAccessUpdateDto dto);
    public Task AddUserAccessAsync(UserAccessAddDto dto);
    public Task UpdateUserAccessAsync(UserAccessUpdateDto dto);
    public Task<string> AddBlockAsync(BlockCreateDto dto);
    public Task UpdateBlockCoordinatesAsync(BlockUpdateCoordinatesDto dto);
    public Task UpdateBlockNameAsync(BlockUpdateNameDto dto);
    public Task DeleteBlockAsync(BlockDeleteDto dto);
    public Task<string> AddReservationAsync(ReservationCreateDto dto);
    public Task DeleteReservationAsync(ReservationDeleteDto dto);
    public Task UpdateUserNameInAllPlacesAsync(string oldUserName, string newUserName);
}