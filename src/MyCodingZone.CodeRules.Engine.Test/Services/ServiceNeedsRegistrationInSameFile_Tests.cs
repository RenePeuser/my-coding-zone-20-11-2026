using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Engine.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class ServiceNeedsRegistrationInSameFile_Tests
    {
        [TestMethod]
        public async Task Should_Report_Service_When_It_Has_No_Registration()
        {
            await Assert.That.CodeRule.ViolatesAsync<ServiceNeedsRegistrationInSameFile>("""
                                                                                         public interface IUserService { void Create(); }

                                                                                         [|public sealed class UserService : IUserService
                                                                                         {
                                                                                             public void Create() { }
                                                                                         }|]
                                                                                         """);
        }

        [TestMethod]
        public async Task Should_Report_Registration_When_It_Is_Not_Named_After_The_Service()
        {
            await Assert.That.CodeRule.ViolatesAsync<ServiceNeedsRegistrationInSameFile>("""
                                                                                         public interface IUserService { void Create(); }

                                                                                         [|internal static class Registrations
                                                                                         {
                                                                                             internal static void AddUsers(this IServiceCollection services)
                                                                                             {
                                                                                                 services.AddSingleton<IUserService, UserService>();
                                                                                             }
                                                                                         }|]

                                                                                         public sealed class UserService : IUserService
                                                                                         {
                                                                                             public void Create() { }
                                                                                         }
                                                                                         """);
        }

        [TestMethod]
        public async Task Should_Accept_Service_When_Its_Registration_Is_In_The_Same_File()
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceNeedsRegistrationInSameFile>("""
                                                                                         public interface IUserService { void Create(); }

                                                                                         internal static class AddUserServiceExtension
                                                                                         {
                                                                                             internal static void AddUserService(this IServiceCollection services)
                                                                                             {
                                                                                                 services.AddSingleton<IUserService, UserService>();
                                                                                             }
                                                                                         }

                                                                                         public sealed class UserService : IUserService
                                                                                         {
                                                                                             public void Create() { }
                                                                                         }
                                                                                         """);
        }

        [TestMethod]
        [DataRow("public static class Defaults { public static void Create() { } }")]
        [DataRow("public abstract class ServiceBase { public void Create() { } }")]
        [DataRow("public sealed class Order : EntityBase { public void Create() { } }")]
        [DataRow("public sealed class Clock { public void Tick() { } } public static class Factory { public static Clock Create() => new Clock(); }")]
        public async Task Should_Accept_Class_When_It_Is_No_Service(string code)
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceNeedsRegistrationInSameFile>(code);
        }

        [TestMethod]
        public async Task Should_Suggest_A_Registration_Named_After_The_Service()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<ServiceNeedsRegistrationInSameFile>("""
                                                                                                           public interface IUserService { void Create(); }

                                                                                                           [|public sealed class UserService : IUserService
                                                                                                           {
                                                                                                               public void Create() { }
                                                                                                           }|]
                                                                                                           """);

            Assert.That.Contains(finding.ShouldBe!,
                                 "services.AddSingleton<IUserService, UserService>();",
                                 because: "The suggested registration registers the service under its interface",
                                 fix: "Check ShouldBe of SERVICES_RULE_001 (ServiceNeedsRegistrationInSameFile)");
        }
    }
}
