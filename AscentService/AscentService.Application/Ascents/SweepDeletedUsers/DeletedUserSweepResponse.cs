namespace AscentService.Application.Ascents.SweepDeletedUsers;

public sealed record DeletedUserSweepResponse(int UsersScanned, int AscentsRemoved);
