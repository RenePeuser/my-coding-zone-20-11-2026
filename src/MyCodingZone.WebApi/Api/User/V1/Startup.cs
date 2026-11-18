using MyCodingZone.WebApi.Api.User.V1.Create;

namespace MyCodingZone.WebApi.Api.User.V1
{
    internal static class Startup
    {
        internal static void AddUsersV1(this IServiceCollection serviceCollection,
                                        IConfiguration _)
        {
            serviceCollection.AddCreateUserEndpoint();
        }
    }
}
