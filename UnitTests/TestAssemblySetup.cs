using NUnit.Framework;

namespace UnitTests;

[SetUpFixture]
public sealed class TestAssemblySetup
{
    [OneTimeSetUp]
    public void RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}