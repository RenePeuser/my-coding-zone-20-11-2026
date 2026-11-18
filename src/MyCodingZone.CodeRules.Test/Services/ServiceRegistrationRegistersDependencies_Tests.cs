using MyCodingZone.CodeRules.Services;

namespace MyCodingZone.CodeRules.Test.Services
{
    [TestClass]
    [TestCategory("CodeRules.Tests")]
    [TestCategory("CodeRules.Tests.Services")]
    public sealed class ServiceRegistrationRegistersDependencies_Tests
    {
        private const string UserRepository = """
                                              public interface IUserRepository { void Save(); }

                                              internal static class AddUserRepositoryExtension
                                              {
                                                  internal static void AddUserRepository(this IServiceCollection services)
                                                  {
                                                      services.AddSingleton<IUserRepository, UserRepository>();
                                                  }
                                              }

                                              public sealed class UserRepository : IUserRepository
                                              {
                                                  public void Save() { }
                                              }
                                              """;

        [TestMethod]
        public async Task Should_Report_Registration_When_It_Forgets_A_Dependency()
        {
            await Assert.That.CodeRule.ViolatesAsync<ServiceRegistrationRegistersDependencies>($$"""
                                                                                                 {{UserRepository}}

                                                                                                 public interface IUserService { void Create(); }

                                                                                                 internal static class AddUserServiceExtension
                                                                                                 {
                                                                                                     [|internal static void AddUserService(this IServiceCollection services)
                                                                                                     {
                                                                                                         services.AddSingleton<IUserService, UserService>();
                                                                                                     }|]
                                                                                                 }

                                                                                                 public sealed class UserService(IUserRepository repository) : IUserService
                                                                                                 {
                                                                                                     public void Create() => repository.Save();
                                                                                                 }
                                                                                                 """);
        }

        [TestMethod]
        [DataRow("services.AddUserRepository();")]
        [DataRow("services.AddSingleton<IUserRepository, UserRepository>();")]
        public async Task Should_Accept_Registration_When_It_Registers_The_Dependency(string dependencyRegistration)
        {
            await Assert.That.CodeRule.CompliesAsync<ServiceRegistrationRegistersDependencies>($$"""
                                                                                                 {{UserRepository}}

                                                                                                 public interface IUserService { void Create(); }

                                                                                                 internal static class AddUserServiceExtension
                                                                                                 {
                                                                                                     internal static void AddUserService(this IServiceCollection services)
                                                                                                     {
                                                                                                         {{dependencyRegistration}}
                                                                                                         services.AddSingleton<IUserService, UserService>();
                                                                                                     }
                                                                                                 }

                                                                                                 public sealed class UserService(IUserRepository repository) : IUserService
                                                                                                 {
                                                                                                     public void Create() => repository.Save();
                                                                                                 }
                                                                                                 """);
        }

        [TestMethod]
        public async Task Should_Suggest_The_Registration_Of_The_Dependency()
        {
            var finding = await Assert.That.CodeRule.ViolatesOnceAsync<ServiceRegistrationRegistersDependencies>($$"""
                                                                                                                   {{UserRepository}}

                                                                                                                   public interface IUserService { void Create(); }

                                                                                                                   internal static class AddUserServiceExtension
                                                                                                                   {
                                                                                                                       [|internal static void AddUserService(this IServiceCollection services)
                                                                                                                       {
                                                                                                                           services.AddSingleton<IUserService, UserService>();
                                                                                                                       }|]
                                                                                                                   }

                                                                                                                   public sealed class UserService(IUserRepository repository) : IUserService
                                                                                                                   {
                                                                                                                       public void Create() => repository.Save();
                                                                                                                   }
                                                                                                                   """);

            Assert.That.AreEqual("services.AddUserRepository();",
                                 finding.ShouldBe,
                                 because: "The finding points to the existing registration of the missing dependency",
                                 fix: "Check ShouldBe of SERVICES_RULE_004 (ServiceRegistrationRegistersDependencies)");
        }
    }
}
