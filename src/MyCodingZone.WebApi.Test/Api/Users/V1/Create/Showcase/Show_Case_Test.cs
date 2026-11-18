using AspNetCore.Simple.MsTest.Sdk;
using MyCodingZone.WebApi.Api.User.V1.Create;
// using MyCodingZone.WebApi.Api.User.V1.Create;

namespace MyCodingZone.WebApi.Test.Api.Users.V1.Create.Showcase
{
    [TestClass]
    [TestCategory("Users")]
    [TestCategory("Users V1")]
    public class Show_Case_Test : ApiTestBase
    {
        [TestMethod]
        [DynamicRequestLocator]
        public Task Should_Be_Able_To_Create_A_New_User(string useCase)
        {
            return Client.AssertPostAsync<CreateUserResponse>("api/v1/users",
                                                              useCase,
                                                              useCase);
        }
    }

}
