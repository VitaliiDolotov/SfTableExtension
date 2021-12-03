using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace SfTableExtension
{
    public static class TableExtension
    {
        public static List<T> Create<T>(this Table table) where T : class, new()
        {
            List<T> collection = new();
            T entry = new();

            var colFields = entry.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
            var colProps = entry.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
            var colData = colFields.Union(colProps);

            foreach (var row in table.CreateSet<T>())
            {
                entry = new T();
                var rowFields = row.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                var rowProps = row.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                var rowData = rowFields.Union(rowProps);

                foreach (var memberInfo in rowData)
                {
                    var firstSuitableCol = colData.First(x => x.Name.Equals(memberInfo.Name));
                    SetMemberValue(firstSuitableCol, entry, memberInfo.GetMemberValue(row));
                }

                if (rowData.Any(x =>
                    (x.GetMemberType().IsPrimitive || x.GetMemberType() == typeof(decimal) ||
                    x.GetMemberType() == typeof(string)) && !x.GetMemberValue(row).Equals(string.Empty)))
                {
                    collection.Add(entry);
                }

                else
                {
                    var entryFields = entry.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                    var entryProps = entry.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                    var entryData = entryFields.Union(entryProps);

                    var collectionLastElementFields = collection.Last().GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                    var collectionLastElementProps = collection.Last().GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                    var collectionLastElementData = collectionLastElementFields.Union(collectionLastElementProps);

                    foreach (var memberInfo in entryData)
                    {
                        var value = memberInfo.GetMemberValue(entry);
                        var collectionFirstSuitableField = collectionLastElementData.First(x => x.Name.Equals(memberInfo.Name));
                        var collectionFirstFieldValue = collectionFirstSuitableField.GetMemberValue(collection.Last());

                        if (collectionFirstSuitableField.GetMemberType().IsArray)
                        {
                            var objectArray = ((IEnumerable)collectionFirstFieldValue).Cast<object>().ToArray();

                            var arrayLength = objectArray.Length;
                            if (arrayLength == 0)
                            {
                                SetMemberValue(collectionFirstSuitableField, collection.Last(), value);
                            }
                            else
                            {
                                var copyArray = Array.CreateInstance(objectArray[0].GetType(), arrayLength + 1);
                                objectArray.CopyTo(copyArray, 0);
                                copyArray.SetValue(value?.GetType().GetMethod("GetValue", new[] { typeof(int) })?.Invoke(value, new object[] { 0 }), arrayLength);
                                SetMemberValue(collectionFirstSuitableField, collection.Last(), copyArray);
                            }
                        }
                        else if (value != null && (!collectionFirstSuitableField.GetMemberType().IsPrimitive || collectionFirstSuitableField.GetMemberType() != typeof(decimal) || collectionFirstSuitableField.GetMemberType() != typeof(string)))
                        {
                            if (value.GetType().GetMethod("get_Count") != null && Convert.ToInt32(value.GetType().GetMethod("get_Count")?.Invoke(value, Array.Empty<object>())) == 0)
                            {
                                break;
                            }

                            var methodInfo = collectionFirstFieldValue.GetType().GetMethod("Add");
                            var elementAt = value.GetType().GetMethod("get_Item")?.Invoke(value, new object[] { 0 });
                            methodInfo?.Invoke(collectionFirstFieldValue, new[] { elementAt });
                        }
                    }
                }
            }
            return collection;
        }

        private static void SetMemberValue(MemberInfo memberInfo, object target, object value)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    ((FieldInfo)memberInfo).SetValue(target, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo)memberInfo).SetValue(target, value, null);
                    break;
                default:
                    throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo",
                        nameof(memberInfo));
            }
        }

        private static object GetMemberValue(this MemberInfo memberInfo, object forObject) =>
            memberInfo.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).GetValue(forObject),
                MemberTypes.Property => ((PropertyInfo)memberInfo).GetValue(forObject, null),
                _ => throw new ArgumentException("MemberInfo must be if type FieldInfo or PropertyInfo"),
            };

        private static Type GetMemberType(this MemberInfo memberInfo) =>
             memberInfo.MemberType switch
             {
                 MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
                 MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                 _ => throw new ArgumentException(
                     "Input MemberInfo must be if type FieldInfo, or PropertyInfo")
             };
    }
}