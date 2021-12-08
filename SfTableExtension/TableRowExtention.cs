using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Tracing;

namespace SfTableExtension
{
    public static class TableRowExtention
    {
        public static TableRow NormalizeTableRow(this TableRow row)
        {
            var headers = new List<string>();
            var values = new List<string>();

            for (int i = 0; i < row.Count; i++)
            {
                var key = NormalizePropertyNameToMatchAgainstAColumnName(
                    RemoveAllCharactersThatAreNotValidInAPropertyName(row.Keys.ElementAt(i)));
                headers.Add(key);
                values.Add(row.Values.ElementAt(i));
            }

            var table = new Table(headers.ToArray());
            table.AddRow(values.ToArray());

            return table.Rows.First();
        }

        #region From SpecFlow

        private static readonly Regex invalidPropertyNameRegex = new Regex(InvalidPropertyNamePattern, RegexOptions.Compiled);
        private const string InvalidPropertyNamePattern = @"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Nd}_]";

        internal static string RemoveAllCharactersThatAreNotValidInAPropertyName(string name)
        {
            //Unicode groups allowed: Lu, Ll, Lt, Lm, Lo, Nl or Nd see https://msdn.microsoft.com/en-us/library/aa664670%28v=vs.71%29.aspx
            return invalidPropertyNameRegex.Replace(name, string.Empty);
        }

        internal static string NormalizePropertyNameToMatchAgainstAColumnName(string name)
        {
            // we remove underscores, because they should be equivalent to spaces that were removed too from the column names
            // we also ignore accents
            return name.Replace("_", string.Empty).ToIdentifier();
        }

        #endregion
    }
}
