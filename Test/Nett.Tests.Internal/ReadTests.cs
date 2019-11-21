using FluentAssertions;
using Xunit;

namespace Nett.Tests.Internal
{
    public class ReadTests
    {
        [Fact]
        public void Fooxxxxyy()
        {
            // Arrange
            const string tml = @"
x = 2
a.a = 3
a.b = 4";

            // Act
            var read = Toml.ReadString(tml);

            // Assert
            read.Get<TomlTable>("a").TableType.Should().Be(TomlTable.TableTypes.Dotted);
        }
    }
}
