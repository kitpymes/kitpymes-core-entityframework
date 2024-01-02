namespace Kitpymes.Core.EntityFramework.Tests
{
    public sealed class FakeRepository(FakeContext context)
        : EntityFrameworkRepository<FakeEntity>(context), IFakeRepository
    {
    }
}
