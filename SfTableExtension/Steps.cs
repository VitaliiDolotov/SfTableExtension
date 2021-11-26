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
        private bool IsCollectionTypeField(FieldInfo fieldInfo)
        {
            return typeof(IEnumerable).IsAssignableFrom(fieldInfo.FieldType) &&
                   !typeof(string).IsAssignableFrom(fieldInfo.FieldType);
        }

        private bool IsArrayTypeField(FieldInfo fieldInfo)
        {
            return typeof(Array).IsAssignableFrom(fieldInfo.FieldType) &&
                   !typeof(string).IsAssignableFrom(fieldInfo.FieldType);
        }

        public List<T> Create<T>(Table table)
        {
            var instances = new List<T>() { table.Rows.First().CreateInstance<T>() };

            var properties = typeof(T).GetProperties().ToList();
            var propertiesList = properties.FindAll(x => typeof(IEnumerable).IsAssignableFrom(x.PropertyType)
                                                         && !typeof(string).IsAssignableFrom(x.PropertyType));

            var fields = typeof(T).GetFields().ToList();
            var fieldsList = fields.FindAll(x => typeof(IEnumerable).IsAssignableFrom(x.FieldType)
                                                 && !typeof(string).IsAssignableFrom(x.FieldType));

            var allListName = propertiesList.Select(x => x.Name).ToList();
            allListName.AddRange(fieldsList.Select(x => x.Name).ToList());

            var allVariablesName = properties.Select(x => x.Name).ToList();
            allVariablesName.AddRange(fields.Select(x => x.Name).ToList());

            foreach (var row in table.Rows.Skip(1))
            {
                bool flag = false;

                foreach (var cell in row)
                {
                    if (allVariablesName.Contains(cell.Key) && !allListName.Contains(cell.Key) && cell.Value.IsNotNullOrEmpty())
                    {
                        T nextInstanse = row.CreateInstance<T>();
                        instances.Add(nextInstanse);
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    continue;
                }

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
                }

                foreach (var field in fieldsList)
                {
                    if (!row.ContainsKey(field.Name))
                        continue;

                    if (IsCollectionTypeField(field))
                    {
                        var fieldStringValue = row[field.Name];

                        if (fieldStringValue.IsNullOrEmpty())
                            continue;

                        object propertyValue = fieldStringValue;

                        var fieldType = IsArrayTypeField(field) ?
                            field.FieldType.GetElementType() :
                            field.FieldType.GenericTypeArguments.First();

                        if (fieldType is null)
                            throw new Exception($"Unable to get {field.Name} property type");

                        // Parse field to the appropriate Type
                        if (fieldType != typeof(string))
                        {
                            try
                            {
                                var parse = fieldType.GetMethods().First(x => x.Name.Equals("Parse"));
                                propertyValue = parse.Invoke(fieldType, new object[] { fieldStringValue });
                            }
                            catch
                            {
                                throw new Exception(
                                    $"Unable to parse '{fieldStringValue}' value of {field.Name} property to {fieldType.Name} type");
                            }
                        }

                        // Add value to array Type filed
                        if (IsArrayTypeField(field))
                        {
                            var getInstanceValue = field.GetValue(instances.Last());
                            var resultList = ((IEnumerable)getInstanceValue).Cast<object>().ToList();
                            resultList.Add(propertyValue);
                            var resultArray = Array.CreateInstance(fieldType, resultList.Count);
                            Array.Copy(resultList.ToArray(), resultArray, resultList.Count);
                            field.SetValue(instances.Last(), resultArray);
                        }
                        // Add value to Collection type filed
                        else
                        {
                            var getInstanceValue = field.GetValue(instances.Last());
                            var iCollectionObject = typeof(ICollection<>).MakeGenericType(fieldType);
                            var addMethod = iCollectionObject.GetMethod("Add");
                            addMethod.Invoke(getInstanceValue, new object[] { propertyValue });
                            field.SetValue(instances.Last(), getInstanceValue);
                        }
                    }
                }
            }

            return instances;
        }
    }
}
