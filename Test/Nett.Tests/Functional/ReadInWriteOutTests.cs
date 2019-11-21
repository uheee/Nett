using FluentAssertions;
using Xunit;

namespace Nett.Tests.Functional
{
    /// <summary>
    /// Tests that show, that data is written back correctly after a read of a file.
    /// </summary>
    public sealed class ReadInNWriteOutTests
    {
        [Theory]
        [InlineData("\"a.value\" = 1")]
        [InlineData("'a.value\\' = 1")]
        public void ReadInNWriteOut_WithNonBareKey_WritesBackWithSameKeyType(string input)
        {
            var written = Toml.WriteString(Toml.ReadString(input));

            written.TrimEnd().Should().Be(input);
        }

        [Fact]
        public void GivenMutlipleDottedKeys_WritesTheTomlCorrectly_SoThatItCanBeReadCorrectlyAfterwards()
        {
            // Arrange
            const string input = @"
[app]
value1 = ""string""
obj.a = 1
obj.b = 2
";

            var tml = Toml.ReadString(input);

            // Act
            var written = Toml.WriteString(tml);

            // Assert
            var readBack = Toml.ReadString(written);
            readBack.Get<TomlTable>("obj").Get<int>("b").Should().Be(2);
        }
    }
}
