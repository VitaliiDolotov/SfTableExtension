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

            var @class = Create<UserAccount>(table);

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

        private bool IsCollectionType<T>(Type propType)
        {
            return typeof(T).IsAssignableFrom(propType) &&
                   !typeof(string).IsAssignableFrom(propType);
        }

        private object Parse(Type propertyType, object propertyValue)
        {
            if (propertyType == typeof(string)) return propertyValue;

            try
            {
                var parse = propertyType.GetMethods().First(x => x.Name.Equals("Parse"));
                return parse.Invoke(propertyType, new object[] { propertyValue });
            }
            catch
            {
                throw new Exception(
                    $"Unable to parse '{propertyValue}' value to {propertyType.Name} type");
            }
        }

        public List<T> Create<T>(Table table)
        {
            var instances = new List<T>() { table.Rows.First().CreateInstance<T>() };

            var properties = typeof(T).GetProperties().ToList();
            var propertiesList = properties.FindAll(x => IsCollectionType<IEnumerable>(x.PropertyType));

            var fields = typeof(T).GetFields().ToList();
            var fieldsList = fields.FindAll(x => IsCollectionType<IEnumerable>(x.FieldType));

            var allCollectionTypeVariablesNames = propertiesList.Select(x => x.Name)
                .Concat(fieldsList.Select(x => x.Name).ToList());

            var allVariablesNames = properties.Select(x => x.Name)
                .Concat(fields.Select(x => x.Name).ToList());

            foreach (var row in table.Rows.Skip(1))
            {
                if (row.Any(cell => allVariablesNames.Contains(cell.Key)
                                    && !allCollectionTypeVariablesNames.Contains(cell.Key) && cell.Value.IsNotNullOrEmpty()))
                {
                    instances.Add(row.CreateInstance<T>());
                    continue;
                }

                foreach (var property in properties)
                {
                    if (!row.ContainsKey(property.Name)) continue;
                    if (!IsCollectionType<IEnumerable>(property.PropertyType)) continue;
                    if (row[property.Name].IsNullOrEmpty()) continue;

                    object propertyValue = row[property.Name];

                    var propertyType = IsCollectionType<Array>(property.PropertyType) ?
                        property.PropertyType.GetElementType() :
                        property.PropertyType.GenericTypeArguments.First();

                    if (propertyType is null)
                        throw new Exception($"Unable to get {property.Name} property type");

                    propertyValue = Parse(propertyType, propertyValue);

                    var getInstanceValue = property.GetValue(instances.Last());
                    // Add value to array Type property
                    if (IsCollectionType<Array>(property.PropertyType))
                    {
                        var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
                        resultList.Add(propertyValue);
                        var resultArray = Array.CreateInstance(propertyType, resultList.Count);
                        Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
                        property.SetValue(instances.Last(), resultArray);
                    }
                    // Add value to Collection type property
                    else
                    {
                        var iCollectionObject = typeof(ICollection<>).MakeGenericType(propertyType);
                        var addMethod = iCollectionObject.GetMethod("Add");
                        addMethod.Invoke(getInstanceValue, new object[] { propertyValue });
                        property.SetValue(instances.Last(), getInstanceValue);
                    }
                }

                foreach (var field in fieldsList)
                {
                    if (!row.ContainsKey(field.Name)) continue;
                    if (!IsCollectionType<IEnumerable>(field.FieldType)) continue;
                    if (row[field.Name].IsNullOrEmpty()) continue;

                    object propertyValue = row[field.Name];

                    var fieldType = IsCollectionType<Array>(field.FieldType) ?
                        field.FieldType.GetElementType() :
                        field.FieldType.GenericTypeArguments.First();

                    if (fieldType is null)
                        throw new Exception($"Unable to get {field.Name} property type");

                    propertyValue = Parse(fieldType, propertyValue);

                    var getInstanceValue = field.GetValue(instances.Last());
                    // Add value to array Type filed
                    if (IsCollectionType<Array>(field.FieldType))
                    {
                        var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
                        resultList.Add(propertyValue);
                        var resultArray = Array.CreateInstance(fieldType, resultList.Count);
                        Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
                        field.SetValue(instances.Last(), resultArray);
                    }
                    // Add value to Collection type filed
                    else
                    {
                        var iCollectionObject = typeof(ICollection<>).MakeGenericType(fieldType);
                        var addMethod = iCollectionObject.GetMethod("Add");
                        addMethod.Invoke(getInstanceValue, new object[] { propertyValue });
                        field.SetValue(instances.Last(), getInstanceValue);
                    }
                }
            }

            return instances;
        }
    }
}
