using Xunit;

namespace AscentService.IntegrationTests;

[CollectionDefinition(nameof(AscentServiceCollection))]
public sealed class AscentServiceCollection : ICollectionFixture<AscentServiceApiFactory>;
