using MultiFactor.Ldap.Adapter.Configuration;

namespace MultiFactor.Ldap.Adapter.Services
{
    public static class RandomWaiterFactory
    {
        public static RandomWaiter CreateWaiter(ServiceConfiguration serviceConfiguration) => new RandomWaiter(serviceConfiguration.InvalidCredentialDelay);
    }
}
