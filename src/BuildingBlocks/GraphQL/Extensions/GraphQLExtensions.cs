using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using BuildingBlocks.GraphQL.Queries;

namespace BuildingBlocks.GraphQL.Extensions;

public static class GraphQLExtensions
{
    public static IServiceCollection AddGraphQLServices(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<OrderQueries>()
            .AddType<OrderType>()
            .AddType<OrderItemType>()
            .AddType<ProductType>()
            .AddType<PaymentType>()
            .AddProjections()
            .AddFiltering()
            .AddSorting()
            .AddInMemorySubscriptions();

        return services;
    }
}
