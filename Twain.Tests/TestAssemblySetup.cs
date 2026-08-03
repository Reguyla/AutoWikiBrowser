using NUnit.Framework;

namespace Twain.Tests;

[SetUpFixture]
public sealed class TestAssemblySetup
{
    [OneTimeSetUp]
    public void RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}