namespace BuildingBlocks;

public sealed class ForbiddenException(string message) : Exception(message)
{
}
