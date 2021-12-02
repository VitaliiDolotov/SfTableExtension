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
            var instances = new List<T>() { table.Rows.First().CreateInstance<T>() };

            var properties = typeof(T).GetProperties().ToList();
            var propertiesList = properties.FindAll(x => IsCollectionType<IEnumerable>(x.PropertyType));

            var fields = typeof(T).GetFields().ToList();
            var fieldsList = fields.FindAll(x => IsCollectionType<IEnumerable>(x.FieldType));

            var allCollectionTypeVariablesNames = propertiesList.Select(x => x.Name)
                .Concat(fieldsList.Select(x => x.Name).ToList());

            var allVariablesNames = properties.Select(x => x.Name)
                .Concat(fields.Select(x => x.Name).ToList());

            var listMember = new List<MemberInfo>();
            listMember.AddRange(fieldsList);
            listMember.AddRange(propertiesList);


            foreach (var row in table.Rows.Skip(1))
            {
                if (row.Any(cell => allVariablesNames.Contains(cell.Key)
                                    && !allCollectionTypeVariablesNames.Contains(cell.Key) && cell.Value.IsNotNullOrEmpty()))
                {
                    instances.Add(row.CreateInstance<T>());
                    continue;
                }

                foreach (var member in listMember)
                {
                    if (!row.ContainsKey(member.Name)) continue;
                    if (!IsCollectionType<IEnumerable>(GetType(member))) continue;
                    if (row[member.Name].IsNullOrEmpty()) continue;

                    object propertyValue = row[member.Name];

                    var propertyType = IsCollectionType<Array>(GetType(member)) ?
                        GetType(member).GetElementType() :
                        GetType(member).GenericTypeArguments.First();

                    if (propertyType is null)
                        throw new Exception($"Unable to get {member.Name} property type");

                    propertyValue = Parse(propertyType, propertyValue);

                    var getValueMethod = GetMethod(member, ValueMethods.GetValue);
                    var getInstanceValue = getValueMethod.Invoke(member, new object?[] { instances.Last() });
                    // Add value to array Type property
                    if (IsCollectionType<Array>(GetType(member)))
                    {
                        var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
                        resultList.Add(propertyValue);
                        var resultArray = Array.CreateInstance(propertyType, resultList.Count);
                        Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
                        var setValueMethod = GetMethod(member, ValueMethods.SetValue);
                        setValueMethod.Invoke(member, new object?[] { instances.Last(), resultArray });
                    }
                    // Add value to Collection type property
                    else
                    {
                        var iCollectionObject = typeof(ICollection<>).MakeGenericType(propertyType);
                        var addMethod = iCollectionObject.GetMethod("Add");
                        addMethod.Invoke(getInstanceValue, new object[] { propertyValue });

                        var setValueMethod = GetMethod(member, ValueMethods.SetValue);
                        setValueMethod.Invoke(member, new object?[] { instances.Last(), getInstanceValue });
                    }
                }
            }

            return instances;
        }

        private static Type GetType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo) member).PropertyType,
                MemberTypes.Field => ((FieldInfo) member).FieldType,
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
                MemberTypes.Property => ((PropertyInfo) member)
                    .GetType().GetMethods().ToList().FindAll(x => x.Name.Equals(method.ToString())).Last(),
                MemberTypes.Field => ((FieldInfo) member)
                    .GetType().GetMethods().ToList().FindAll(x => x.Name.Equals(method.ToString())).Last(),
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
            if (propertyType == typeof(string)) return propertyValue;

            if (propertyType.BaseType == (typeof(Enum)))
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
