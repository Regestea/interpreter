namespace interpreter.Api.Tests.Fixture;

/// <summary>
/// Collection definition to ensure tests run sequentially and not in parallel
/// </summary>
[CollectionDefinition("Whisper Collection")]
public class WhisperCollection : ICollectionFixture<WhisperServiceFixture>
{
}