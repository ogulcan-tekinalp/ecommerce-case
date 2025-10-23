namespace BuildingBlocks.Messaging.Events;

public record OrderItemDto(Guid ProductId, int Quantity, decimal Price);