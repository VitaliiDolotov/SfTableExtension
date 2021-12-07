using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;
using Xunit;

namespace SfTableExtension
{
    public class Test
    {
        [Fact]
        public void Check_Table_Creation()
        {
            var table = new Table("AccountScore", "Name", "Phone", "Roles", "RoleActivationStatus", "Languages");
            table.AddRow("5", "Marian", "08477542984", "Portal Admin", "true", "English");
            table.AddRow("2", string.Empty, string.Empty, "Sales person", "false", "Swedish");
            table.AddRow("8", string.Empty, string.Empty, "Content moderator", "false", "Finnish");
            table.AddRow(string.Empty, "Tom", "08473322911", string.Empty, "true", "Finnish");
            table.AddRow("7", string.Empty, string.Empty, "Portal Admin", "false", "Swedish");
            table.AddRow("1", string.Empty, string.Empty, "Content moderator", "true", string.Empty);

            var @class = table.Create<UserAccount>();

            // First object
            @class.First().Roles[0].Should().Be("Portal Admin");
            @class.First().Roles[1].Should().Be("Sales person");
            @class.First().Roles[2].Should().Be("Content moderator");

            @class.First().RoleActivationStatus[0].Should().Be(true);
            @class.First().RoleActivationStatus[1].Should().Be(false);
            @class.First().RoleActivationStatus[2].Should().Be(false);

            @class.First().AccountScore.ToList()[0].Should().Be(5);
            @class.First().AccountScore.ToList()[1].Should().Be(2);
            @class.First().AccountScore.ToList()[2].Should().Be(8);

            @class.First().Languages[0].Should().Be(Language.English);
            @class.First().Languages[1].Should().Be(Language.Swedish);
            @class.First().Languages[2].Should().Be(Language.Finnish);

            @class.First().TruncatedName.Should().Be("Mar");

            // Second object
            @class.Last().Roles[0].Should().Be("Portal Admin");
            @class.Last().Roles[1].Should().Be("Content moderator");

            @class.Last().RoleActivationStatus[0].Should().Be(true);
            @class.Last().RoleActivationStatus[1].Should().Be(false);
            @class.Last().RoleActivationStatus[2].Should().Be(true);

            @class.Last().AccountScore.ToList()[0].Should().Be(7);
            @class.Last().AccountScore.ToList()[1].Should().Be(1);

            @class.Last().Languages[0].Should().Be(Language.Finnish);
            @class.Last().Languages[1].Should().Be(Language.Swedish);

            @class.Last().TruncatedName.Should().Be("Tom");
        }
    }
}
