using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Nett.Tests.Util;
using Xunit;

namespace Nett.Coma.Tests.Functional
{
    public sealed class InlineTableTest
    {
        public class Cfg
        {
            [TreatAsInlineTable]
            public class Items : Dictionary<string, bool> { };

            public Items UserItems { get; set; }
        }

        [Fact]
        public void Write_WithInlineTableProperty_WritesThatTableAsInlineTable()
        {
            using (var file = TestFileName.Create("Input", ".toml"))
            {
                // Arrange
                const string expected = @"UserItems = { X = true, Y = false }
";
                const string input = "UserItems = { 'X' = false, 'Y' = true }";
                File.WriteAllText(file, input);
                var toWrite = new Cfg.Items()
                {
                    { "X", true },
                    { "Y", false }
                };

                // Act
                var cfg = Config.CreateAs()
                    .MappedToType(() => new Cfg())
                    .StoredAs(store => store.File(file))
                    .Initialize();
                cfg.Set(c => c.UserItems, toWrite);

                // Assert
                var f = File.ReadAllText(file);
                f.ShouldBeNormalizedEqualTo(expected);
            }
        }

        [Fact]
        public void Read_WithInlineTableProperty_ReadsThatPropertyCorrectly()
        {
            using (var file = TestFileName.Create("input", ".toml"))
            {
                // Arrange
                const string input = "UserItems = { 'X' = false, 'Y' = true }";
                File.WriteAllText(file, input);

                // Act
                var cfg = Config.CreateAs()
                    .MappedToType(() => new Cfg())
                    .StoredAs(store => store.File(file))
                    .Initialize();
                var items = cfg.Get(c => c.UserItems);

                // Assert
                items.Count.Should().Be(2);
                items["X"].Should().Be(false);
                items["Y"].Should().Be(true);
            }
        }

        public class GenericDictConfig
        {
            public Dictionary<string, object> Table { get; set; } = new Dictionary<string, object>();
        }


        [Fact]
        public void GivenGenericDictStructure_CanReadItAsObject()
        {
            using var machine = TestFileName.Create("machine", ".toml");

            // Arrange
            const string machineText = @"
[Table]
a = 1
d = ""D""
e = [""b"", ""c""]";

            File.WriteAllText(machine, machineText);

            var cfg = Config.CreateAs()
                .MappedToType(() => new GenericDictConfig())
                .StoredAs(store => store.File(machine))
                .Initialize();

            // Act
            var r = cfg.Get(c => c.Table["e"]);

            // Assert
            ((object[])r).OfType<string>().Should().Equal("b", "c");
        }

        public class FooyCfg
        {
            public Dictionary<string, object> Tbl { get; set; } = new Dictionary<string, object>();
        }

        [Fact]
        public void Fooy()
        {
            using var machine = TestFileName.Create("machine", ".toml");
            using var user = TestFileName.Create("user", ".toml");

            // Arrange
            const string machineText = @"[Tbl]
x = 1";
            const string userText = @"[Tbl]
x = 2";
            File.WriteAllText(machine, machineText);
            File.WriteAllText(user, userText);

            // Act
            IConfigSource src = null;
            var cfg = Config.CreateAs()
                .MappedToType(() => new FooyCfg())
                .StoredAs(store => store.File(machine)
                    .MergeWith(store.File(user).AccessedBySource("user", out src)))
                .Initialize();

            var tbl = cfg.Get(c => c.Tbl);
            tbl["x"] = 4;
            cfg.Set(c => c.Tbl, tbl, src);

            // Assert
            File.ReadAllText(user).ShouldBeSemanticallyEquivalentTo(@"[Tbl]
x = 4");
        }
    }
}
