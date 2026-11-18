using AspNetCore.Simple.MsTest.Sdk;
using Siemens.AspNet.ErrorHandling.Contracts;

namespace MyCodingZone.WebApi.Test.Api.Users.V1.Create.InvalidRequests
{
    // Problem: First level support reports that a client is not able to create a user.
    //          What does the client get back, and can it fix the request on its own?
    //
    // The Siemens enterprise error handler (Siemens.AspNet.ErrorHandling) answers every invalid body with a
    // ValidationProblemDetailsExtended: the broken property, its current value, the allowed samples
    // (e.g. all enum values) and the position in the JSON. The snapshots in Responses/ show the full answer.
    [TestClass]
    [TestCategory("Users")]
    [TestCategory("Users V1")]
    public class Invalid_Request_Test : ApiTestBase
    {
        [TestMethod]
        [DynamicRequestLocator]
        public Task Should_Explain_Why_The_User_Cannot_Be_Created(string useCase)
        {
            return Client.AssertPostAsErrorAsync<ValidationProblemDetailsExtended>("api/v1/users",
                                                                                    useCase,
                                                                                    useCase);
        }
    }
}
