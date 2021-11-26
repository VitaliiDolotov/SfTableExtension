using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using TechTalk.SpecRun.Common.Helper;
using Xunit;

namespace SfTableExtension
{
    public class Test
    {
        [Fact]
        public void Check_Table_Creation()
        {
            var table = new Table("AccountScore", "Name", "Phone", "Roles", "RoleActivationStatus");
            table.AddRow("5", "Marian", "08477542984", "Portal Admin", "true");
            table.AddRow("2", string.Empty, string.Empty, "Sales person", "false");
            table.AddRow("8", string.Empty, string.Empty, "Content moderator", "false");
            table.AddRow(string.Empty, "Tom", "08473322911", string.Empty, "true");
            table.AddRow("7", string.Empty, string.Empty, "Portal Admin", "false");
            table.AddRow("1", string.Empty, string.Empty, "Content moderator", "true");

            var @class = table.Create<UserAccount>();

            // First object
            @class.First().Roles[0].Should().Be("Portal Admin");
            @class.First().Roles[1].Should().Be("Sales person");
            @class.First().Roles[2].Should().Be("Content moderator");

            @class.First().RoleActivationStatus[0].Should().Be(true);
            @class.First().RoleActivationStatus[1].Should().Be(false);
            @class.First().RoleActivationStatus[2].Should().Be(false);

            @class.First().AccountScore[0].Should().Be(5);
            @class.First().AccountScore[1].Should().Be(2);
            @class.First().AccountScore[2].Should().Be(8);

            // Second object
            @class.Last().Roles[0].Should().Be("Portal Admin");
            @class.Last().Roles[1].Should().Be("Content moderator");

            @class.Last().RoleActivationStatus[0].Should().Be(true);
            @class.Last().RoleActivationStatus[1].Should().Be(false);
            @class.Last().RoleActivationStatus[2].Should().Be(true);

            @class.Last().AccountScore[0].Should().Be(7);
            @class.Last().AccountScore[1].Should().Be(1);
        }
    }
}
