using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace Binary.Serialization
{
    public static class BinarySerializer
    {
        #region Serialize
        public static byte[] Serialize(object obj)
        {

            if (obj == null) throw new ArgumentNullException(nameof(obj));

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    SerializeValue(writer, obj);
                }
                return memoryStream.ToArray();
            }
        }

        public static void Serialize(Stream output, object obj)
        {

            if (obj == null) throw new ArgumentNullException(nameof(obj));

            using (var writer = new BinaryWriter(output))
            {
                SerializeValue(writer, obj);
            }
        }
        private static void SerializeObject(BinaryWriter writer, object obj)
        {
            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            SerializeValue(writer, fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsPublic && !fields[i].IsStatic && !fields[i].IsLiteral && !fields[i].IsNotSerialized)
                {
                    SerializeValue(writer, fields[i].Name);
                    SerializeValue(writer, fields[i].GetValue(obj));
                }
            }
            SerializeValue(writer, properties.Length);
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].CanRead && properties[i].CanWrite)
                {
                    SerializeValue(writer, properties[i].Name);
                    SerializeValue(writer, properties[i].GetValue(obj));
                }
            }
        }
        private static void SerializeValue(BinaryWriter writer, object value)
        {
            if (value == null)
            {
                writer.Write(0);
                return;
            }
            Type type = value.GetType();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                if (type == typeof(string))
                {
                    writer.Write((string)value);
                }
                else if (type == typeof(decimal))
                {
                    writer.Write((decimal)value);
                }
                else if (type == typeof(short))
                {
                    writer.Write((short)value);
                }
                else if (type == typeof(int))
                {
                    writer.Write((int)value);
                }
                else if (type == typeof(long))
                {
                    writer.Write((long)value);
                }
                else if (type == typeof(ushort))
                {
                    writer.Write((ushort)value);
                }
                else if (type == typeof(uint))
                {
                    writer.Write((uint)value);
                }
                else if (type == typeof(ulong))
                {
                    writer.Write((ulong)value);
                }
                else if (type == typeof(double))
                {
                    writer.Write((double)value);
                }
                else if (type == typeof(float))
                {
                    writer.Write((float)value);
                }
                else if (type == typeof(byte))
                {
                    writer.Write((byte)value);
                }
                else if (type == typeof(sbyte))
                {
                    writer.Write((sbyte)value);
                }
                else if (type == typeof(char))
                {
                    writer.Write((char)value);
                }
                else if (type == typeof(bool))
                {
                    writer.Write((bool)value);
                }
            }
            else if (type == typeof(DateTime))
            {
                DateTime dateTimeValue = (DateTime)value;
                writer.Write(dateTimeValue.ToBinary());
            }
            else if (type.IsEnum)
            {
                writer.Write(value.ToString());
            }
            else if (value is IEnumerable && value is ICollection)
            {
                IEnumerable enumerable = value as IEnumerable;
                ICollection collection = value as ICollection;
                writer.Write(collection.Count);
                IEnumerator e = enumerable.GetEnumerator();
                while (e.MoveNext())
                {
                    SerializeValue(writer, e.Current);
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                SerializeValue(writer, type.GetProperty("Key").GetValue(value, BindingFlags.Default, null, null, null));
                SerializeValue(writer, type.GetProperty("Value").GetValue(value, BindingFlags.Default, null, null, null));
            }
            else
            {
                SerializeObject(writer, value);
            }
        }

        #endregion
        public static T Deserialize<T>(byte[] bytes)
        {
            return (T)Deserialize(typeof(T), bytes);
        }
        public static object Deserialize(Type type, byte[] bytes)
        {
            int length = bytes.Length;
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (length < 0 || length > bytes.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    return DeserializeValue(reader, type);
                }
            }
        }

        private static object DeserializeObject(BinaryReader reader, Type type)
        {
            try
            {
                object result;
                if (type.IsValueType)
                {
                    result = Activator.CreateInstance(type);
                }
                else
                {
                    result = FormatterServices.GetUninitializedObject(type);
                }
                if (result != null)
                {
                    int length = reader.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        string name = reader.ReadString();
                        FieldInfo field = type.GetField(name);
                        if (field != null)
                        {
                            field.SetValue(result, DeserializeValue(reader, field.FieldType));
                        }
                    }
                    length = reader.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        string name = reader.ReadString();
                        PropertyInfo property = type.GetProperty(name);

                        if (property != null)
                        {
                            property.SetValue(result, DeserializeValue(reader, property.PropertyType));
                        }
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                return null;
            }

        }

        private static object DeserializeValue(BinaryReader reader, Type type)
        {
            try
            {
                object result = null;
                if (type == null)
                {
                }
                if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                {
                    if (type == typeof(string))
                    {
                        result = reader.ReadString();
                    }
                    else if (type == typeof(decimal))
                    {
                        result = reader.ReadDecimal();
                    }
                    else if (type == typeof(short))
                    {
                        return reader.ReadInt16();
                    }
                    else if (type == typeof(int))
                    {
                        return reader.ReadInt32();
                    }
                    else if (type == typeof(long))
                    {
                        return reader.ReadInt64();
                    }
                    else if (type == typeof(ushort))
                    {
                        return reader.ReadUInt16();
                    }
                    else if (type == typeof(uint))
                    {
                        return reader.ReadUInt32();
                    }
                    else if (type == typeof(ulong))
                    {
                        return reader.ReadUInt64();
                    }
                    else if (type == typeof(double))
                    {
                        return reader.ReadDouble();
                    }
                    else if (type == typeof(float))
                    {
                        return reader.ReadSingle();
                    }
                    else if (type == typeof(byte))
                    {
                        return reader.ReadByte();
                    }
                    else if (type == typeof(sbyte))
                    {
                        return reader.ReadSByte();
                    }
                    else if (type == typeof(char))
                    {
                        return reader.ReadChar();
                    }
                    else if (type == typeof(bool))
                    {
                        return reader.ReadBoolean();
                    }
                }
                else if (type == typeof(DateTime))
                {
                    return DateTime.FromBinary(reader.ReadInt64());
                }
                else if (type.IsEnum)
                {
                    result = Enum.Parse(type, reader.ReadString());
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    IList list = (IList)type.GetConstructor(Type.EmptyTypes).Invoke(null);
                    int length = reader.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        list.Add(DeserializeValue(reader, genericArgs[0]));
                    }
                    result = list;
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    PropertyInfo key = type.GetProperty("Key");
                    PropertyInfo value = type.GetProperty("Value");
                    key.SetValue(result, DeserializeValue(reader, key.PropertyType));
                    value.SetValue(reader, DeserializeValue(reader, value.PropertyType));
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    IDictionary dic = (IDictionary)type.GetConstructor(Type.EmptyTypes).Invoke(null);
                    int length = reader.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        dic.Add(DeserializeValue(reader, genericArgs[0]), DeserializeValue(reader, genericArgs[1]));
                    }
                    result = dic;
                }
                else
                {
                    result = DeserializeObject(reader, type);
                }
                return result;
            }
            catch (Exception e)
            {
                return null;

            }
        }
    }

}