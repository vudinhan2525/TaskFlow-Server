using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public static class ConverterUtils
{

    // Example input: 
    // {
    //     name: "An Vu",
    //     age: null,
    // }
    // Output: 
    // {
    //     name: "An Vu",
    // }
    public static Dictionary<string, object?> MakeDataUpdate(DataUpdateInput input)
    {
        if (input.Data == null) throw new ArgumentNullException(nameof(input.Data));

        // mặc định loại bỏ: null, chuoi rong
        input.RemoveValues ??= new List<object?> { null, "" };

        var result = new Dictionary<string, object?>();

        var props = input.Data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            // nếu field nằm trong removeFields thì bỏ qua
            if (input.RemoveFields != null && input.RemoveFields.Contains(prop.Name))
            {
                continue;
            }

            object? value = prop.GetValue(input.Data);

            bool shouldRemove = false;

            foreach (var rv in input.RemoveValues)
            {
                if (rv == null && value == null )
                {
                    shouldRemove = true;
                    break;
                }

                if (rv is string s1 && value is string s2 && s1 == s2)
                {
                    shouldRemove = true;
                    break;
                }

                if (rv is Array && value is Array arr2 && arr2.Length == 0)
                {
                    shouldRemove = true;
                    break;
                }

                if (rv is IEnumerable && value is IEnumerable e && !e.GetEnumerator().MoveNext())
                {
                    shouldRemove = true;
                    break;
                }
            }

            if (!shouldRemove)
            {
                if ((value as string)?.ToUpper()=="NULL")
                {
                    result[prop.Name] = "";
                }else{

                result[prop.Name] = value;
                }
            }
        }
        
        return result;
    }
    public static DateTime? ParseIsoDateTime(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        return DateTime.Parse(input, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }
    public class DataUpdateInput
    {
        public object Data { get; set; } = default!;

        // Giá trị cần bỏ (mặc định: null, "")
        public List<object?>? RemoveValues { get; set; } = null;

        // Các field (property name) cần bỏ
        public List<string>? RemoveFields { get; set; } = null;
    }
}
