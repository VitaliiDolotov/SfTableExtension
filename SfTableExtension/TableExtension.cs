using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using TechTalk.SpecRun.Common.Helper;

namespace SfTableExtension
{
    public static class TableExtension
    {
        public static List<T> Create<T>(this Table table)
        {
            var instances = new List<T>() { table.Rows.First().CreateInstance<T>() };

            var properties = typeof(T).GetProperties().ToList();
            var propertiesList = properties.FindAll(x => IsCollectionType<IEnumerable>(x.PropertyType)).Select(x => (MemberInfo)x).ToList(); ;

            var fields = typeof(T).GetFields().ToList();
            var fieldsList = fields.FindAll(x => IsCollectionType<IEnumerable>(x.FieldType)).Select(x => (MemberInfo)x); ;

            var allCollectionTypeVariablesNames = propertiesList.Select(x => x.Name)
                .Concat(fieldsList.Select(x => x.Name).ToList());

            var allVariablesNames = properties.Select(x => x.Name)
                .Concat(fields.Select(x => x.Name).ToList());

            var propsAndFields = propertiesList.Concat(fieldsList).ToList();

            foreach (var row in table.Rows.Skip(1))
            {
                if (row.Any(cell => allVariablesNames.Contains(cell.Key)
                                    && !allCollectionTypeVariablesNames.Contains(cell.Key) && cell.Value.IsNotNullOrEmpty()))
                {
                    instances.Add(row.CreateInstance<T>());
                    continue;
                }

                foreach (var property in propsAndFields)
                {
                    if (!row.ContainsKey(((MemberInfo)property).Name)) continue;
                    if (!IsCollectionType<IEnumerable>(PropertyType(property))) continue;
                    if (row[((MemberInfo)property).Name].IsNullOrEmpty()) continue;

                    object propertyValue = row[((MemberInfo)property).Name];

                    var propertyType = IsCollectionType<Array>(PropertyType(property)) ?
                        PropertyType(property).GetElementType() :
                        PropertyType(property).GenericTypeArguments.First();

                    if (propertyType is null)
                        throw new Exception($"Unable to get {((MemberInfo)property).Name} property type");

                    propertyValue = Parse(propertyType, propertyValue);

                    var getInstanceValue = property.GetValue(instances.Last());
                    // Add value to array Type property
                    if (IsCollectionType<Array>(PropertyType(property)))
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
            }

            return instances;
        }

        private static Type PropertyType(MemberInfo memberInfo)
        {
            return (Type)memberInfo.GetType().GetProperties(BindingFlags.Public |
                                                            BindingFlags.Instance |
                                                            BindingFlags.DeclaredOnly).Last(x => x.PropertyType == typeof(Type)).GetValue(memberInfo, null);
        }

        private static bool IsCollectionType<T>(Type propType)
        {
            return typeof(T).IsAssignableFrom(propType) &&
                   !typeof(string).IsAssignableFrom(propType);
        }

        private static object Parse(Type propertyType, object propertyValue)
        {
            if (propertyType == typeof(string)) return propertyValue;

            if (propertyType.BaseType.Equals((typeof(Enum))))
                return Enum.Parse(propertyType, propertyValue.ToString());

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
    }
}
