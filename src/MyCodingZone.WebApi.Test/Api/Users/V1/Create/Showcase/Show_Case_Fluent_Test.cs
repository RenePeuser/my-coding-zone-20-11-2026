using System.Net;
using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.WebApi.Api.User.V1.Create;

namespace MyCodingZone.WebApi.Test.Api.Users.V1.Create.Showcase
{
    /// <summary>
    ///     Same test cases as <see cref="Show_Case_Test" />, written with the fluent API.
    ///     Both share the Requests/ and Responses/ snapshots of this folder.
    /// </summary>
    [TestClass]
    [TestCategory("Users")]
    [TestCategory("Users V1")]
    public class Show_Case_Fluent_Test : ApiTestBase
    {
        [TestMethod]
        [DynamicRequestLocator]
        public Task Should_Be_Able_To_Create_A_New_User(string useCase)
        {
            return Client.AssertPost("api/v1/users")
                         .AcceptsFromEmbeddedJson(useCase)
                         .Produces<CreateUserResponse>(HttpStatusCode.OK)
                         .ExpectedResponseFromEmbeddedJson(useCase)
                         .ExecuteAsync();
        }
    }
}
