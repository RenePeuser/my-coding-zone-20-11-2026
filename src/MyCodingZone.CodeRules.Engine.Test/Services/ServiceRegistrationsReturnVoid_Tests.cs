using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Engine.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class ServiceRegistrationsReturnVoid_Tests
    {
        [TestMethod]
        public async Task Should_Report_Registration_When_It_Returns_IServiceCollection()
        {
            await Assert.That.CodeRule.ViolatesAsync<ServiceRegistrationsReturnVoid>("""
                                                                                     internal static class AddUserServiceExtension
                                                                                     {
                                                                                         [|internal static IServiceCollection AddUserService(this IServiceCollection services)
                                                                                         {
                                                                                             return services.AddSingleton<UserService>();
                                                                                         }|]
                                                                                     }
                                                                                     """);
        }

        [TestMethod]
        [DataRow("internal static void AddUserService(this IServiceCollection services) { services.AddSingleton<UserService>(); }")]
        [DataRow("internal static IServiceCollection Configure(this IServiceCollection services) => services;")]
        [DataRow("internal static int AddNumbers(int a, int b) => a + b;")]
        public async Task Should_Accept_Method_When_It_Is_A_Void_Registration_Or_No_Registration(string method)
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceRegistrationsReturnVoid>($$"""
                                                                                       internal static class AddUserServiceExtension
                                                                                       {
                                                                                           {{method}}
                                                                                       }
                                                                                       """);
        }

        [TestMethod]
        public async Task Should_Name_The_Return_Type_In_Details()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<ServiceRegistrationsReturnVoid>("""
                                                                                                       internal static class AddUserServiceExtension
                                                                                                       {
                                                                                                           [|internal static IServiceCollection AddUserService(this IServiceCollection services)
                                                                                                           {
                                                                                                               return services.AddSingleton<UserService>();
                                                                                                           }|]
                                                                                                       }
                                                                                                       """);

            Assert.That.AreEqual("IServiceCollection",
                                 finding.Details.Single(detail => detail.Key == "Returns").Value?.ToString(),
                                 because: "The finding names the return type that has to become 'void'",
                                 fix: "Check the Details of SERVICES_RULE_005 (ServiceRegistrationsReturnVoid)");
        }
    }
}
