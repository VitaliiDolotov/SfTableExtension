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
    [Binding]
    class Steps : SpecFlowContext
    {
        [Given(@"User generates object with list")]
        public void GivenUserGeneratesObjectWithList(Table table)
        {
            var user = Create<UserAccount>(table).First();
        }

        private bool IsCollectionTypeProperty(PropertyInfo propertyInfo)
        {
            return typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) &&
                   !typeof(string).IsAssignableFrom(propertyInfo.PropertyType);
        }

        private bool IsArrayTypeProperty(PropertyInfo propertyInfo)
        {
            return typeof(Array).IsAssignableFrom(propertyInfo.PropertyType) &&
                   !typeof(string).IsAssignableFrom(propertyInfo.PropertyType);
        }

        public static List<T> Create<T>(Table table)
        {
            var instances = new List<T>() { table.Rows[0].CreateInstance<T>() };

            var properties = typeof(T).GetProperties().ToList();
            var propertiesList = properties.FindAll(x => typeof(IEnumerable)
                .IsAssignableFrom(x.PropertyType) && !typeof(string).IsAssignableFrom(x.PropertyType));

            var fields = typeof(T).GetFields().ToList();
            var fieldsList = fields.FindAll(x => typeof(IEnumerable)
                .IsAssignableFrom(x.FieldType) && !typeof(string).IsAssignableFrom(x.FieldType));

            var allListName = propertiesList.Select(x => x.Name).ToList();
            allListName.AddRange(fieldsList.Select(x => x.Name).ToList());

            var allVariablesName = properties.Select(x => x.Name).ToList();
            allVariablesName.AddRange(fields.Select(x => x.Name).ToList());

            foreach (var row in table.Rows.Skip(1))
            {
                foreach (var property in properties)
                {
                    if (row.ContainsKey(property.Name))
                    {
                        if (propertiesList.Contains(property))
                        {
                            var getSubInstanceValue = row[property.Name];
                            if (string.IsNullOrEmpty(getSubInstanceValue))
                            {
                                continue;
                            }
                            object result;
                            Type subInstanceType = default;
                            object getInstanceValue = default;
                            if (typeof(Array).IsAssignableFrom(property.PropertyType))
                            {
                                getInstanceValue = property.GetValue(instances.Last()) as object[];
                                subInstanceType = getInstanceValue.GetType().GetElementType();
                            }
                            else
                            {
                                getInstanceValue = property.GetValue(instances.Last());
                                subInstanceType = property.PropertyType.GenericTypeArguments.First();
                            }
                            if (subInstanceType != typeof(string))
                            {
                                try
                                {
                                    var parse = subInstanceType.GetMethods().First(x => x.Name.Equals("Parse"));
                                    result = parse.Invoke(subInstanceType, new object[] { getSubInstanceValue });
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception($"Impossible to convert string into '{subInstanceType.Name}' type. Exception: '{ex}'.");
                                }
                            }
                            else
                            {
                                result = getSubInstanceValue;
                            }
                            if (typeof(Array).IsAssignableFrom(property.PropertyType))
                            {
                            }
                            else
                            {
                                var iCollectionObject = typeof(ICollection<>).MakeGenericType(subInstanceType);
                                var addMethod = iCollectionObject.GetMethod("Add");
                                addMethod.Invoke(getInstanceValue, new object[] { result });
                                property.SetValue(instances.Last(), getInstanceValue);
                            }
                        }
                    }
                }

                foreach (var field in fields)
                {
                    if (row.ContainsKey(field.Name))
                    {
                        if (fieldsList.Contains(field))
                        {
                            var getSubInstanceValue = row[field.Name];
                            if (string.IsNullOrEmpty(getSubInstanceValue))
                            {
                                continue;
                            }
                            object result;
                            Type subInstanceType = default;
                            object getInstanceValue = default;
                            if (typeof(Array).IsAssignableFrom(field.FieldType))
                            {
                                getInstanceValue = field.GetValue(instances.Last()) as object[];
                                subInstanceType = getInstanceValue.GetType().GetElementType();
                            }
                            else
                            {
                                getInstanceValue = field.GetValue(instances.Last());
                                subInstanceType = field.FieldType.GenericTypeArguments.First();
                            }
                            if (subInstanceType != typeof(string))
                            {
                                try
                                {
                                    var parse = subInstanceType.GetMethods().First(x => x.Name.Equals("Parse"));
                                    result = parse.Invoke(subInstanceType, new object[] { getSubInstanceValue });
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception($"Impossible to convert string into '{subInstanceType.Name}' type. Exception: '{ex}'.");
                                }
                            }
                            else
                            {
                                result = getSubInstanceValue;
                            }
                            if (typeof(Array).IsAssignableFrom(field.FieldType))
                            {
                            }
                            else
                            {
                                var iCollectionObject = typeof(ICollection<>).MakeGenericType(subInstanceType);
                                var addMethod = iCollectionObject.GetMethod("Add");
                                addMethod.Invoke(getInstanceValue, new object[] { result });
                                field.SetValue(instances.Last(), getInstanceValue);
                            }
                        }
                    }
                }

                foreach (var cell in row)
                {
                    if (allVariablesName.Contains(cell.Key) && !allListName.Contains(cell.Key) && cell.Value.IsNotNullOrEmpty())
                    {
                        T nextInstanse = row.CreateInstance<T>();
                        instances.Add(nextInstanse);
                        break;
                    }
                }
            }

            return instances;
        }

        //public List<T> Create<T>(Table table)
        //{
        //    var instances = new List<T>() { table.Rows.First().CreateInstance<T>() };

        //    var properties = typeof(T).GetProperties().ToList();

        //    foreach (var row in table.Rows.Skip(1))
        //    {
        //        foreach (var property in properties)
        //        {
        //            if (!row.ContainsKey(property.Name))
        //                continue;

        //            if (IsCollectionTypeProperty(property))
        //            {
        //                var propertyStringValue = row[property.Name];

        //                if (propertyStringValue.IsNullOrEmpty())
        //                    continue;

        //                object propertyValue = propertyStringValue;

        //                var propertyType = IsArrayTypeProperty(property) ?
        //                    property.PropertyType.GetElementType() :
        //                    property.PropertyType.GenericTypeArguments.First();

        //                if (propertyType is null)
        //                    throw new Exception($"Unable to get {property.Name} property type");

        //                // Parse property to the appropriate Type
        //                if (propertyType != typeof(string))
        //                {
        //                    try
        //                    {
        //                        var parse = propertyType.GetMethods().First(x => x.Name.Equals("Parse"));
        //                        propertyValue = parse.Invoke(propertyType, new object[] { propertyStringValue });
        //                    }
        //                    catch
        //                    {
        //                        throw new Exception(
        //                            $"Unable to parse '{propertyStringValue}' value of {property.Name} property to {propertyType.Name} type");
        //                    }
        //                }

        //                // Add value to array Type property
        //                if (IsArrayTypeProperty(property))
        //                {
        //                    var getInstanceValue = property.GetValue(instances.Last());
        //                    var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
        //                    resultList.Add(propertyValue);
        //                    var resultArray = Array.CreateInstance(propertyType, resultList.Count);
        //                    Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
        //                    property.SetValue(instances.Last(), resultArray);
        //                }
        //                // Add value to Collection type property
        //                else
        //                {
        //                    var getInstanceValue = property.GetValue(instances.Last());
        //                    var iCollectionObject = typeof(ICollection<>).MakeGenericType(propertyType);
        //                    var addMethod = iCollectionObject.GetMethod("Add");
        //                    addMethod.Invoke(getInstanceValue, new object[] { propertyValue });
        //                    property.SetValue(instances.Last(), getInstanceValue);
        //                }
        //            }
        //            else
        //            {
        //                if (row[property.Name].IsNotNullOrEmpty())
        //                {
        //                    instances.Add(row.CreateInstance<T>());
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    return instances;
        //}
    }
}
