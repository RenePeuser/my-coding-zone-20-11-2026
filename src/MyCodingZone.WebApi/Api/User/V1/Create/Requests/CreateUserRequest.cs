namespace MyCodingZone.WebApi.Api.User.V1.Create
{
    public record CreateUserRequest
    {
        public required string FirstName { get; init; }

        public required string LastName { get; init; }

        public required string Email { get; set; }

        public required UserType UserType { get; init; }
    }
}
