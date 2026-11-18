using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class ServiceRegistrationRegistersTheServiceItself_Tests
    {
        [TestMethod]
        [DataRow("// services.AddSingleton<IUserService, UserService>();")]
        [DataRow("services.AddSingleton<IOtherService, OtherService>();")]
        [DataRow("")]
        public async Task Should_Report_Registration_When_It_Does_Not_Register_The_Service(string registration)
        {
            await Assert.That.CodeRule.ViolatesAsync<ServiceRegistrationRegistersTheServiceItself>($$"""
                                                                                                     public interface IUserService { void Create(); }

                                                                                                     internal static class AddUserServiceExtension
                                                                                                     {
                                                                                                         [|internal static void AddUserService(this IServiceCollection services)
                                                                                                         {
                                                                                                             {{registration}}
                                                                                                         }|]
                                                                                                     }

                                                                                                     public sealed class UserService : IUserService
                                                                                                     {
                                                                                                         public void Create() { }
                                                                                                     }
                                                                                                     """);
        }

        [TestMethod]
        [DataRow("services.AddSingleton<IUserService, UserService>();")]
        [DataRow("services.AddScoped<UserService>();")]
        [DataRow("services.AddHostedService<UserService>();")]
        public async Task Should_Accept_Registration_When_It_Registers_The_Service(string registration)
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceRegistrationRegistersTheServiceItself>($$"""
                                                                                                     public interface IUserService { void Create(); }

                                                                                                     internal static class AddUserServiceExtension
                                                                                                     {
                                                                                                         internal static void AddUserService(this IServiceCollection services)
                                                                                                         {
                                                                                                             {{registration}}
                                                                                                         }
                                                                                                     }

                                                                                                     public sealed class UserService : IUserService
                                                                                                     {
                                                                                                         public void Create() { }
                                                                                                     }
                                                                                                     """);
        }

        [TestMethod]
        public async Task Should_Suggest_The_Registration_Under_The_Interface()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<ServiceRegistrationRegistersTheServiceItself>("""
                                                                                                                     public interface IUserService { void Create(); }

                                                                                                                     internal static class AddUserServiceExtension
                                                                                                                     {
                                                                                                                         [|internal static void AddUserService(this IServiceCollection services)
                                                                                                                         {
                                                                                                                         }|]
                                                                                                                     }

                                                                                                                     public sealed class UserService : IUserService
                                                                                                                     {
                                                                                                                         public void Create() { }
                                                                                                                     }
                                                                                                                     """);

            Assert.That.AreEqual("services.AddSingleton<IUserService, UserService>();",
                                 finding.ShouldBe,
                                 because: "The finding shows the registration of the service under its interface",
                                 fix: "Check ShouldBe of SERVICES_RULE_007 (ServiceRegistrationRegistersTheServiceItself)");
        }
    }
}
