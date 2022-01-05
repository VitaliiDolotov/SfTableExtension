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
            var table = new Table("Account Score", "Name", "Phone", "Roles", "Role_Activation.Status", "Languages");
            table.AddRow("5.6", "Marian", "08477542984", "Portal, Admin", "true, false", "English");
            table.AddRow("2", string.Empty, string.Empty, "Sales person", "true", "Swedish");
            table.AddRow("8", string.Empty, string.Empty, "Content moderator", "false", "Finnish");
            table.AddRow(string.Empty, "Tom", "08473322911", string.Empty, "true", "Finnish");
            table.AddRow("7", string.Empty, string.Empty, "Portal Admin", "false", "Swedish");
            table.AddRow("1", string.Empty, string.Empty, "Content moderator", "true", string.Empty);

            var userAccounts = table.Create<UserAccount>();

            // First object
            userAccounts.First().Roles[0].Should().Be("Portal, Admin");
            userAccounts.First().Roles[1].Should().Be("Sales person");
            userAccounts.First().Roles[2].Should().Be("Content moderator");

            userAccounts.First().RoleActivationStatus[0].Should().Be(true);
            userAccounts.First().RoleActivationStatus[1].Should().Be(false);
            userAccounts.First().RoleActivationStatus[2].Should().Be(true);
            userAccounts.First().RoleActivationStatus[3].Should().Be(false);

            userAccounts.First().AccountScore.ToList()[0].Should().Be(5.6);
            userAccounts.First().AccountScore.ToList()[1].Should().Be(2);
            userAccounts.First().AccountScore.ToList()[2].Should().Be(8);

            userAccounts.First().Languages[0].Should().Be(Language.English);
            userAccounts.First().Languages[1].Should().Be(Language.Swedish);
            userAccounts.First().Languages[2].Should().Be(Language.Finnish);

            userAccounts.First().TruncatedName.Should().Be("Mar");

            // Second object
            userAccounts.Last().Roles[0].Should().Be("Portal Admin");
            userAccounts.Last().Roles[1].Should().Be("Content moderator");

            userAccounts.Last().RoleActivationStatus[0].Should().Be(true);
            userAccounts.Last().RoleActivationStatus[1].Should().Be(false);
            userAccounts.Last().RoleActivationStatus[2].Should().Be(true);

            userAccounts.Last().AccountScore.ToList()[0].Should().Be(7);
            userAccounts.Last().AccountScore.ToList()[1].Should().Be(1);

            userAccounts.Last().Languages[0].Should().Be(Language.Finnish);
            userAccounts.Last().Languages[1].Should().Be(Language.Swedish);

            userAccounts.Last().TruncatedName.Should().Be("Tom");
        }
    }
}
