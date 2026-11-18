using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class ServiceRegistrationMustBeCalled_Tests
    {
        private const string UserService = """
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
                                           """;

        [TestMethod]
        public async Task Should_Report_Registration_When_Nobody_Calls_It()
        {
            await Assert.That.CodeRule.ViolatesAsync<ServiceRegistrationMustBeCalled>("""
                                                                                      public interface IUserService { void Create(); }

                                                                                      internal static class AddUserServiceExtension
                                                                                      {
                                                                                          [|internal static void AddUserService(this IServiceCollection services)
                                                                                          {
                                                                                              services.AddSingleton<IUserService, UserService>();
                                                                                          }|]
                                                                                      }

                                                                                      public sealed class UserService : IUserService
                                                                                      {
                                                                                          public void Create() { }
                                                                                      }
                                                                                      """);
        }

        [TestMethod]
        public async Task Should_Accept_Registration_When_The_Parent_Feature_Calls_It()
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceRegistrationMustBeCalled>($$"""
                                                                                        {{UserService}}

                                                                                        internal static class UsersStartup
                                                                                        {
                                                                                            internal static void AddUsers(this IServiceCollection services)
                                                                                            {
                                                                                                services.AddUserService();
                                                                                            }
                                                                                        }
                                                                                        """);
        }

        [TestMethod]
        public async Task Should_Accept_Registration_When_Program_Cs_Calls_It()
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceRegistrationMustBeCalled>([TestFile.Production("UserService.cs", UserService),
                                                                                       TestFile.Production("Program.cs", "builder.Services.AddUserService();")]);
        }

        [TestMethod]
        public async Task Should_Suggest_The_Call_Of_The_Registration()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<ServiceRegistrationMustBeCalled>("""
                                                                                                        public interface IUserService { void Create(); }

                                                                                                        internal static class AddUserServiceExtension
                                                                                                        {
                                                                                                            [|internal static void AddUserService(this IServiceCollection services)
                                                                                                            {
                                                                                                                services.AddSingleton<IUserService, UserService>();
                                                                                                            }|]
                                                                                                        }

                                                                                                        public sealed class UserService : IUserService
                                                                                                        {
                                                                                                            public void Create() { }
                                                                                                        }
                                                                                                        """);

            Assert.That.AreEqual("services.AddUserService();",
                                 finding.ShouldBe,
                                 because: "The finding shows the call that registers the service",
                                 fix: "Check ShouldBe of SERVICES_RULE_002 (ServiceRegistrationMustBeCalled)");
        }
    }
}
