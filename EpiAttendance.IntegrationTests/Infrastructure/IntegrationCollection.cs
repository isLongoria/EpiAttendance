using Xunit;

namespace EpiAttendance.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ApiFactory> { }
