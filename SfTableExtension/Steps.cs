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

        public List<T> Create<T>(Table table)
        {
            var instances = new List<T>() { table.Rows.First().CreateInstance<T>() };

            var properties = typeof(T).GetProperties().ToList();

            foreach (var row in table.Rows.Skip(1))
            {
                foreach (var property in properties)
                {
                    if (!row.ContainsKey(property.Name))
                        continue;

                    if (IsCollectionTypeProperty(property))
                    {
                        var propertyStringValue = row[property.Name];

                        if (propertyStringValue.IsNullOrEmpty())
                            continue;

                        object propertyValue = propertyStringValue;

                        var propertyType = IsArrayTypeProperty(property) ?
                            property.PropertyType.GetElementType() :
                            property.PropertyType.GenericTypeArguments.First();

                        if (propertyType is null)
                            throw new Exception($"Unable to get {property.Name} property type");

                        // Parse property to the appropriate Type
                        if (propertyType != typeof(string))
                        {
                            try
                            {
                                var parse = propertyType.GetMethods().First(x => x.Name.Equals("Parse"));
                                propertyValue = parse.Invoke(propertyType, new object[] { propertyStringValue });
                            }
                            catch
                            {
                                throw new Exception(
                                    $"Unable to parse '{propertyStringValue}' value of {property.Name} property to {propertyType.Name} type");
                            }
                        }

                        // Add value to array Type property
                        if (IsArrayTypeProperty(property))
                        {
                            var getInstanceValue = property.GetValue(instances.Last());
                            var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
                            resultList.Add(propertyValue);
                            var resultArray = Array.CreateInstance(propertyType, resultList.Count);
                            Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
                            property.SetValue(instances.Last(), resultArray);
                        }
                        // Add value to Collection type property
                        else
                        {
                            var getInstanceValue = property.GetValue(instances.Last());
                            var iCollectionObject = typeof(ICollection<>).MakeGenericType(propertyType);
                            var addMethod = iCollectionObject.GetMethod("Add");
                            addMethod.Invoke(getInstanceValue, new object[] { propertyValue });
                            property.SetValue(instances.Last(), getInstanceValue);
                        }
                    }
                    else
                    {
                        if (row[property.Name].IsNotNullOrEmpty())
                        {
                            instances.Add(row.CreateInstance<T>());
                            break;
                        }
                    }
                }
            }

            return instances;
        }
    }
}
