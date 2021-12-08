using System;
using System.Collections;
using System.Collections.Generic;
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
            var instances = new List<T>();

            var properties = typeof(T).GetProperties().ToList();
            var collectionTypeProperties = properties
                .FindAll(x => IsCollectionType<IEnumerable>(x.PropertyType));

            var fields = typeof(T).GetFields().ToList();
            var collectionTypeFields = fields
                .FindAll(x => IsCollectionType<IEnumerable>(x.FieldType));

            var allCollectionTypeVariablesNames = collectionTypeProperties
                .Select(x => x.Name)
                .Concat(collectionTypeFields.Select(x => x.Name).ToList());

            var allVariablesNames = properties.Select(x => x.Name)
                .Concat(fields.Select(x => x.Name).ToList());

            var collectionFieldsAndProperties = new List<MemberInfo>();
            collectionFieldsAndProperties.AddRange(collectionTypeFields);
            collectionFieldsAndProperties.AddRange(collectionTypeProperties);

            foreach (var row in table.Rows)
            {
                var normalizeRow = row.NormalizeTableRow();

                if (normalizeRow.Any(cell => allVariablesNames.Contains(cell.Key) &&
                                             !allCollectionTypeVariablesNames.Contains(cell.Key) &&
                                             cell.Value.IsNotNullOrEmpty()))
                {
                    var newInstance = normalizeRow.CreateInstance<T>();
                    newInstance = VerifyCollectionElements(newInstance, collectionFieldsAndProperties, normalizeRow);
                    instances.Add(newInstance);
                    continue;
                }

                foreach (var member in collectionFieldsAndProperties)
                {
                    if (!normalizeRow.ContainsKey(member.Name)) continue;
                    if (normalizeRow[member.Name].IsNullOrEmpty()) continue;

                    object propertyValue = normalizeRow[member.Name];

                    var propertyType = IsCollectionType<Array>(GetType(member)) ?
                        GetType(member).GetElementType() :
                        GetType(member).GenericTypeArguments.First();

                    if (propertyType is null)
                        throw new Exception($"Unable to get {member.Name} property type");

                    propertyValue = Parse(propertyType, propertyValue);

                    var getValueMethod = GetMethod(member, ValueMethods.GetValue);
                    var getInstanceValue = getValueMethod.Invoke(member, new object[] { instances.Last() });
                    var setValueMethod = GetMethod(member, ValueMethods.SetValue);
                    // Add value to array Type property
                    if (IsCollectionType<Array>(GetType(member)))
                    {
                        var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
                        resultList.Add(propertyValue);
                        var resultArray = Array.CreateInstance(propertyType, resultList.Count);
                        Array.Copy(resultList.ToArray(), resultArray, resultList.Count);

                        setValueMethod.Invoke(member, new object[] { instances.Last(), resultArray });
                    }
                    // Add value to Collection type property
                    else
                    {
                        var iCollectionObject = typeof(ICollection<>).MakeGenericType(propertyType);
                        var addMethod = iCollectionObject.GetMethod("Add");
                        addMethod.Invoke(getInstanceValue, new object[] { propertyValue });

                        setValueMethod.Invoke(member, new object[] { instances.Last(), getInstanceValue });
                    }
                }
            }

            return instances;
        }

        public static T VerifyCollectionElements<T>(T instance, List<MemberInfo> collection, TableRow row)
        {
            foreach (var member in collection)
            {
                var getValueMethod = GetMethod(member, ValueMethods.GetValue);
                var instanceValue = getValueMethod.Invoke(member, new object[] { instance });
                var valueCount = ((IEnumerable)instanceValue)?.Cast<object>().ToList();

                if (!(valueCount?.Count > 1)) continue;

                var value = row[member.Name];
                var setValueMethod = GetMethod(member, ValueMethods.SetValue);
                var addMethod = instanceValue.GetType().GetMethod("Add");

                if (IsCollectionType<Array>(GetType(member)))
                {
                    var resultList = new List<object> { value };
                    var resultArray = new string[resultList.Count];
                    Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
                    setValueMethod.Invoke(member, new object[] { instance, resultArray });
                }
                else
                {
                    var clearMethod = instanceValue.GetType().GetMethod("Clear");
                    clearMethod?.Invoke(instanceValue, Array.Empty<object>());
                    addMethod?.Invoke(instanceValue, new object[] { value });
                    setValueMethod.Invoke(member, new object[] { instance, instanceValue });
                }
            }

            return instance;
        }

        private static Type GetType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                _ => throw new Exception($"Not supported memberInfo type. MemberInfo type: {member.MemberType}")
            };
        }

        private enum ValueMethods
        {
            GetValue,
            SetValue
        }

        private static MethodInfo GetMethod(MemberInfo member, ValueMethods method)
        {
            return member.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo)member)
                    .GetType()
                    .GetMethods()
                    .ToList()
                    .FindAll(x => x.Name.Equals(method.ToString()))
                    .Last(),
                MemberTypes.Field => ((FieldInfo)member)
                    .GetType()
                    .GetMethods()
                    .ToList()
                    .FindAll(x => x.Name.Equals(method.ToString()))?
                    .Last(),
                _ => throw new Exception($"Not supported memberInfo type. MemberInfo type: {member.MemberType}")
            };
        }

        private static bool IsCollectionType<T>(Type propType)
        {
            return typeof(T).IsAssignableFrom(propType) &&
                   !typeof(string).IsAssignableFrom(propType);
        }

        private static object Parse(Type propertyType, object propertyValue)
        {
            if (propertyType == typeof(string))
                return propertyValue;

            if (propertyType.BaseType == (typeof(Enum)))
            {
                var parsedValue = Enum.Parse(propertyType, propertyValue.ToString());
                return parsedValue;
            }

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
