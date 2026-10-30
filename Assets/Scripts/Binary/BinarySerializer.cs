#if UNITY_EDITOR
#nullable enable

using System.Text;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Scripts.Binary
{
    public static class BinarySerializer
    {
        private const uint GenericMagic = 0x314E4942;
        private const int GenericFormatVersion = 1;
        private const int MaxEntries = 10_000_000;

        public static byte[] Serialize<T>(T? value)
        {
            using MemoryStream ms = new();
            using (BinaryWriter writer = new(ms))

            Serialize(writer, value);
            return ms.ToArray();
        }

        public static byte[] Serialize(object value)
        {
            using MemoryStream ms = new();
            using (BinaryWriter writer = new(ms))
            {
                WriteGenericHeader(writer);
                WriteValue(writer, value, value.GetType() ?? typeof(object));
            }

            return ms.ToArray();
        }

        public static void Serialize<T>(Stream stream, T? value)
        {
            using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
            Serialize(writer, value);
        }

        public static void Serialize<T>(BinaryWriter writer, T? value)
        {
            WriteGenericHeader(writer);
            WriteValue(writer, value, typeof(T));
        }

        public static void WritePayload<T>(BinaryWriter writer, T? value)
            => WriteValue(writer, value, typeof(T));

        public static void SerializeToFile<T>(string path, T? value)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(path, Serialize(value));
        }

        public static async Task SerializeToFileAsync<T>(string path, T? value, CancellationToken ct = default)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllBytesAsync(path, Serialize(value), ct).ConfigureAwait(false);
        }

        public static T? Deserialize<T>(byte[] bytes)
        {
            using MemoryStream ms = new(bytes, writable: false);
            using BinaryReader reader = new(ms);
            return Deserialize<T>(reader);
        }

        public static T? Deserialize<T>(Stream stream)
        {
            using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
            return Deserialize<T>(reader);
        }

        public static T? Deserialize<T>(BinaryReader reader)
        {
            ReadGenericHeader(reader);
            object? obj = ReadValue(reader, typeof(T));
            if (obj is null)
                return default;

            return (T?)obj;
        }

        public static object? Deserialize(byte[] bytes, Type type)
        {
            using MemoryStream ms = new(bytes, writable: false);
            using BinaryReader reader = new(ms);
            ReadGenericHeader(reader);
            return ReadValue(reader, type);
        }

        public static T? ReadPayload<T>(BinaryReader reader)
        {
            object? obj = ReadValue(reader, typeof(T));
            if (obj is null)
                return default;

            return (T?)obj;
        }

        public static void ReadPayloadInto<T>(BinaryReader reader, T instance) where T : class =>
            PopulateInto(reader, instance, typeof(T));

        public static T? DeserializeFromFile<T>(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Bin file not found: {path}", path);

            return Deserialize<T>(File.ReadAllBytes(path));
        }

        public static async Task<T?> DeserializeFromFileAsync<T>(string path, CancellationToken ct = default)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Bin file not found: {path}", path);

            byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
            return Deserialize<T>(bytes);
        }

        public static bool TryDeserializeFromFile<T>(string path, out T? value)
        {
            value = default;
            try
            {
                if (!File.Exists(path))
                    return false;

                value = Deserialize<T>(File.ReadAllBytes(path));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteGenericHeader(BinaryWriter writer)
        {
            writer.Write(GenericMagic);
            writer.Write(GenericFormatVersion);
        }

        private static void ReadGenericHeader(BinaryReader reader)
        {
            uint magic;
            try
            {
                magic = reader.ReadUInt32();
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Truncated bin file (missing header).", ex);
            }

            if (magic != GenericMagic)
                throw new InvalidDataException($"Invalid bin file (bad magic 0x{magic:X8}).");

            int version = reader.ReadInt32();
            if (version != GenericFormatVersion)
                throw new InvalidDataException($"Unsupported bin version: {version}.");
        }

        private static void WriteValue(BinaryWriter writer, object? value, Type declaredType)
        {
            if (value is null)
            {
                if (IsNonNullableValueType(declaredType))
                    throw new InvalidOperationException($"Cannot serialize null for non-nullable '{declaredType.FullName}'.");

                writer.Write((byte)BinKind.Null);
                return;
            }

            Type actual = value.GetType();
            Type effectiveDeclared = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

            if (NeedsRuntimeTypeName(effectiveDeclared, actual))
            {
                writer.Write((byte)BinKind.RuntimeTyped);
                writer.Write(actual.AssemblyQualifiedName ?? actual.FullName ?? actual.Name);
                WriteInner(writer, value, actual);
                return;
            }

            WriteInner(writer, value, actual);
        }

        private static bool NeedsRuntimeTypeName(Type declared, Type actual)
        {
            if (declared == typeof(object))
                return true;

            if (declared.IsInterface || declared.IsAbstract)
                return true;

            return declared != actual;
        }

        private static void WriteInner(BinaryWriter writer, object value, Type actual)
        {
            if (actual.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(actual);
                object flat = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture)!;
                WriteInner(writer, flat, underlying);
                return;
            }

            if (actual == typeof(bool))
            {
                writer.Write((byte)BinKind.Bool);
                writer.Write((bool)value);
                return;
            }

            if (actual == typeof(byte))
            {
                writer.Write((byte)BinKind.Byte);
                writer.Write((byte)value);
                return;
            }

            if (actual == typeof(sbyte))
            {
                writer.Write((byte)BinKind.SByte);
                writer.Write((sbyte)value);
                return;
            }

            if (actual == typeof(short))
            {
                writer.Write((byte)BinKind.Int16);
                writer.Write((short)value);
                return;
            }

            if (actual == typeof(ushort))
            {
                writer.Write((byte)BinKind.UInt16);
                writer.Write((ushort)value);
                return;
            }

            if (actual == typeof(int))
            {
                writer.Write((byte)BinKind.Int32);
                writer.Write((int)value);
                return;
            }

            if (actual == typeof(uint))
            {
                writer.Write((byte)BinKind.UInt32);
                writer.Write((uint)value);
                return;
            }

            if (actual == typeof(long))
            {
                writer.Write((byte)BinKind.Int64);
                writer.Write((long)value);
                return;
            }

            if (actual == typeof(ulong))
            {
                writer.Write((byte)BinKind.UInt64);
                writer.Write((ulong)value);
                return;
            }

            if (actual == typeof(float))
            {
                writer.Write((byte)BinKind.Float);
                writer.Write((float)value);
                return;
            }

            if (actual == typeof(double))
            {
                writer.Write((byte)BinKind.Double);
                writer.Write((double)value);
                return;

            }

            if (actual == typeof(decimal))
            {
                writer.Write((byte)BinKind.Decimal);
                writer.Write((decimal)value);
                return;
            }

            if (actual == typeof(char))
            {
                writer.Write((byte)BinKind.Char);
                writer.Write((char)value);
                return;
            }

            if (actual == typeof(string))
            {
                writer.Write((byte)BinKind.String);
                writer.Write((string)value);
                return;
            }

            if (actual == typeof(DateTime))
            {
                DateTime dt = (DateTime)value;
                writer.Write((byte)BinKind.DateTime);
                writer.Write(dt.Ticks);
                writer.Write((byte)dt.Kind);
                return;
            }

            if (actual == typeof(TimeSpan))
            {
                writer.Write((byte)BinKind.TimeSpan);
                writer.Write(((TimeSpan)value).Ticks);
                return;
            }

            if (actual == typeof(Guid))
            {
                writer.Write((byte)BinKind.Guid);
                byte[] raw = ((Guid)value).ToByteArray();
                writer.Write(raw, 0, raw.Length);
                return;
            }

            if (actual == typeof(DateTimeOffset))
            {
                DateTimeOffset dto = (DateTimeOffset)value;
                writer.Write((byte)BinKind.DateTimeOffset);
                writer.Write(dto.Ticks);
                writer.Write((short)dto.Offset.TotalMinutes);
                return;
            }

            if (actual == typeof(Uri))
            {
                writer.Write((byte)BinKind.Uri);
                writer.Write(((Uri)value).ToString());
                return;
            }

            if (actual == typeof(byte[]))
            {
                byte[] arr = (byte[])value;
                writer.Write((byte)BinKind.ByteArray);
                writer.Write(arr.Length);
                writer.Write(arr, 0, arr.Length);
                return;
            }

            if (TryGetDictionaryTypes(actual, out Type? keyType, out Type? valueType))
            {
                writer.Write((byte)BinKind.Dictionary);
                List<DictionaryEntry> entries = new();
                foreach (DictionaryEntry e in (IDictionary)value)
                    entries.Add(e);

                if (entries.Count == 0 && value is IEnumerable enumerable)
                {
                    foreach (object item in enumerable)
                    {
                        if (item is null)
                            continue;

                        Type it = item.GetType();
                        if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                        {
                            object? k = it.GetProperty("Key")?.GetValue(item);
                            object? v = it.GetProperty("Value")?.GetValue(item);
                            if (k is null)
                                throw new InvalidDataException("Null dictionary key.");

                            entries.Add(new DictionaryEntry(k, v));
                        }
                    }
                }

                writer.Write(entries.Count);
                foreach (DictionaryEntry e in entries)
                {
                    WriteValue(writer, e.Key, keyType!);
                    WriteValue(writer, e.Value, valueType!);
                }

                return;
            }

            if (TryGetEnumerableElementType(actual, out Type? elemType))
            {
                writer.Write((byte)BinKind.List);
                if (value is ICollection coll)
                {
                    writer.Write(coll.Count);
                    foreach (object item in coll)
                        WriteValue(writer, item, elemType!);
                }
                else
                {
                    List<object> tmp = new();
                    foreach (object item in (IEnumerable)value)
                        tmp.Add(item);

                    writer.Write(tmp.Count);
                    foreach (object item in tmp)
                        WriteValue(writer, item, elemType!);
                }
                return;
            }

            writer.Write((byte)BinKind.Object);
            WriteObjectPayload(writer, value, actual);
        }

        private static object? ReadValue(BinaryReader reader, Type declaredType)
        {
            BinKind kind;
            try
            {
                kind = (BinKind)reader.ReadByte();
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Truncated bin file (unexpected end of stream).", ex);
            }

            if (kind == BinKind.Null)
            {
                if (IsNonNullableValueType(declaredType))
                    throw new InvalidDataException($"Null value for non-nullable '{declaredType.FullName}'.");

                return null;
            }

            if (kind == BinKind.RuntimeTyped)
            {
                string aqn = reader.ReadString();
                Type runtime = BinaryTypeNames.Resolve(aqn) ?? throw new InvalidDataException($"Unknown runtime type '{aqn}'.");
                object? inner = ReadValue(reader, runtime);
                if (inner is null)
                    return null;

                Type effective = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
                if (effective == typeof(object) || effective.IsAssignableFrom(inner.GetType()))
                    return inner;

                try
                {
                    return Convert.ChangeType(inner, effective, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Cannot assign '{inner.GetType().FullName}' to '{declaredType.FullName}'.", ex);
                }
            }

            return ReadInner(reader, kind, declaredType);
        }

        private static object? ReadInner(BinaryReader reader, BinKind kind, Type declaredType)
        {
            Type target = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            bool targetIsObject = target == typeof(object);

            switch (kind)
            {
                case BinKind.Bool:
                    return ConvertToTarget(reader.ReadBoolean(), declaredType, target, targetIsObject);

                case BinKind.Byte:
                    return ConvertToTarget(reader.ReadByte(), declaredType, target, targetIsObject);

                case BinKind.SByte:
                    return ConvertToTarget(reader.ReadSByte(), declaredType, target, targetIsObject);

                case BinKind.Int16:
                    return ConvertToTarget(reader.ReadInt16(), declaredType, target, targetIsObject);

                case BinKind.UInt16:
                    return ConvertToTarget(reader.ReadUInt16(), declaredType, target, targetIsObject);

                case BinKind.Int32:
                    return ConvertToTarget(reader.ReadInt32(), declaredType, target, targetIsObject);

                case BinKind.UInt32:
                    return ConvertToTarget(reader.ReadUInt32(), declaredType, target, targetIsObject);

                case BinKind.Int64:
                    return ConvertToTarget(reader.ReadInt64(), declaredType, target, targetIsObject);

                case BinKind.UInt64:
                    return ConvertToTarget(reader.ReadUInt64(), declaredType, target, targetIsObject);

                case BinKind.Float:
                    return ConvertToTarget(reader.ReadSingle(), declaredType, target, targetIsObject);

                case BinKind.Double:
                    return ConvertToTarget(reader.ReadDouble(), declaredType, target, targetIsObject);

                case BinKind.Decimal:
                    return ConvertToTarget(reader.ReadDecimal(), declaredType, target, targetIsObject);

                case BinKind.Char:
                    return ConvertToTarget(reader.ReadChar(), declaredType, target, targetIsObject);

                case BinKind.String:
                    {
                        string s = reader.ReadString();
                        if (targetIsObject || target == typeof(string))
                            return s;

                        if (target.IsEnum)
                            return Enum.Parse(target, s, ignoreCase: true);

                        if (target == typeof(Guid))
                            return Guid.Parse(s);

                        if (target == typeof(TimeSpan))
                            return TimeSpan.Parse(s, CultureInfo.InvariantCulture);

                        if (target == typeof(DateTime))
                            return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                        if (target == typeof(DateTimeOffset))
                            return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);

                        if (target == typeof(Uri))
                            return new Uri(s);

                        if (target == typeof(Version))
                            return Version.Parse(s);

                        return Convert.ChangeType(s, target, CultureInfo.InvariantCulture);
                    }

                case BinKind.DateTime:
                    {
                        long ticks = reader.ReadInt64();
                        DateTimeKind dk = (DateTimeKind)reader.ReadByte();
                        DateTime dt = new(ticks, dk);
                        return ConvertToTarget(dt, declaredType, target, targetIsObject);
                    }

                case BinKind.TimeSpan:
                    {
                        TimeSpan ts = new(reader.ReadInt64());
                        return ConvertToTarget(ts, declaredType, target, targetIsObject);
                    }

                case BinKind.Guid:
                    {
                        byte[] raw = ReadBytesExact(reader, 16);
                        Guid g = new(raw);
                        return ConvertToTarget(g, declaredType, target, targetIsObject);
                    }

                case BinKind.DateTimeOffset:
                    {
                        long ticks = reader.ReadInt64();
                        short offsetMin = reader.ReadInt16();
                        DateTimeOffset dto = new(ticks, TimeSpan.FromMinutes(offsetMin));
                        return ConvertToTarget(dto, declaredType, target, targetIsObject);
                    }

                case BinKind.Uri:
                    {
                        string s = reader.ReadString();
                        if (targetIsObject || target == typeof(Uri))
                            return new Uri(s);

                        return Convert.ChangeType(s, target, CultureInfo.InvariantCulture);
                    }

                case BinKind.ByteArray:
                    {
                        int len = reader.ReadInt32();
                        CheckCount(len);
                        byte[] raw = ReadBytesExact(reader, len);
                        if (targetIsObject || target == typeof(byte[]) || target.IsAssignableFrom(typeof(byte[])))
                            return raw;

                        throw new InvalidDataException($"Cannot assign byte[] to '{declaredType.FullName}'.");
                    }

                case BinKind.List:
                    return ReadList(reader, declaredType, target);

                case BinKind.Dictionary:
                    return ReadDictionary(reader, declaredType, target);

                case BinKind.Object:
                    return ReadObject(reader, target);

                default:
                    throw new InvalidDataException($"Unknown bin kind tag {(byte)kind}.");
            }
        }

        private static object ConvertToTarget(object raw, Type declaredType, Type target, bool targetIsObject)
        {
            if (targetIsObject)
                return raw;

            Type rawType = raw.GetType();
            if (target.IsAssignableFrom(rawType))
                return raw;

            if (target.IsEnum)
            {
                if (raw is string s)
                    return Enum.Parse(target, s, ignoreCase: true);

                return Enum.ToObject(target, raw);
            }

            if (target == typeof(TimeSpan) && raw is string ts)
                return TimeSpan.Parse(ts, CultureInfo.InvariantCulture);

            if (target == typeof(Guid) && raw is string gs)
                return Guid.Parse(gs);

            if (target == typeof(DateTime) && raw is string ds)
                return DateTime.Parse(ds, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            try
            {
                return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Cannot convert '{rawType.FullName}' to '{declaredType.FullName}'.", ex);
            }
        }

        private static object ReadList(BinaryReader reader, Type declaredType, Type target)
        {
            int count = reader.ReadInt32();
            CheckCount(count);

            Type elemType = TryGetEnumerableElementType(target, out Type? et) && et is not null ? et : TryGetEnumerableElementType(declaredType, out Type? et2) && et2 is not null ? et2 : typeof(object);

            List<object?> items = new(count);
            for (int i = 0; i < count; i++)
                items.Add(ReadValue(reader, elemType));

            if (target.IsArray)
            {
                Type arrElem = target.GetElementType() ?? typeof(object);
                Array arr = Array.CreateInstance(arrElem, count);
                for (int i = 0; i < count; i++)
                    arr.SetValue(ConvertForCollection(items[i], arrElem), i);

                return arr;
            }

            Type listType = typeof(List<>).MakeGenericType(elemType);
            if (target == listType || target.IsAssignableFrom(listType))
            {
                object list = Activator.CreateInstance(listType)!;
                MethodInfo add = listType.GetMethod("Add", new[] { elemType })!;
                foreach (object? item in items)
                    add.Invoke(list, new[] { ConvertForCollection(item, elemType) });

                if (target == listType || target.IsAssignableFrom(list.GetType()))
                    return list;
            }

            Type concrete = target.IsInterface || target.IsAbstract ? listType : target;
            if (concrete.IsArray)
                return items.ToArray();

            object instance;
            try
            {
                instance = Activator.CreateInstance(concrete, nonPublic: true)!;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Cannot create collection '{target.FullName}'. Add a parameterless constructor.", ex);
            }

            if (instance is IList ilist)
            {
                foreach (object? item in items)
                    ilist.Add(ConvertForCollection(item, elemType));

                return instance;
            }

            MethodInfo? addMethod = concrete.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance) ?? concrete.GetMethod("Enqueue", BindingFlags.Public | BindingFlags.Instance) ?? concrete.GetMethod("Push", BindingFlags.Public | BindingFlags.Instance);
            if (addMethod is not null && addMethod.GetParameters().Length == 1)
            {
                Type pType = addMethod.GetParameters()[0].ParameterType;
                foreach (object? item in items)
                    addMethod.Invoke(instance, new[] { ConvertForCollection(item, pType) });

                return instance;
            }

            throw new InvalidDataException($"Collection '{target.FullName}' has no Add/Enqueue/Push method.");
        }

        private static object? ConvertForCollection(object? value, Type dest)
        {
            if (value is null)
                return dest.IsValueType && Nullable.GetUnderlyingType(dest) is null ? Activator.CreateInstance(dest) : null;

            if (dest.IsAssignableFrom(value.GetType()))
                return value;

            Type t = Nullable.GetUnderlyingType(dest) ?? dest;
            if (t.IsEnum)
                return Enum.ToObject(t, value);

            return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
        }

        private static object ReadDictionary(BinaryReader reader, Type declaredType, Type target)
        {
            int count = reader.ReadInt32();
            CheckCount(count);

            (Type kType, Type vType) dictTypes;
            if (TryGetDictionaryTypes(target, out Type? k1, out Type? v1) && k1 is not null && v1 is not null)
            {
                dictTypes = (k1, v1);
            }
            else if (TryGetDictionaryTypes(declaredType, out Type? k2, out Type? v2) && k2 is not null && v2 is not null)
            {
                dictTypes = (k2, v2);
            }
            else
                dictTypes = (typeof(object), typeof(object));

            Type concrete = target.IsInterface || target.IsAbstract ? typeof(Dictionary<,>).MakeGenericType(dictTypes.kType, dictTypes.vType) : target;

            object instance;
            try
            {
                instance = Activator.CreateInstance(concrete, nonPublic: true)!;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Cannot create dictionary '{target.FullName}'. Add a parameterless constructor.", ex);
            }

            MethodInfo? add = concrete.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            if (add is null || add.GetParameters().Length != 2)
            {
                if (instance is IDictionary d)
                {
                    for (int i = 0; i < count; i++)
                    {
                        object? k = ReadValue(reader, dictTypes.kType);
                        object? v = ReadValue(reader, dictTypes.vType);
                        d.Add(k!, v);
                    }

                    return instance;
                }

                throw new InvalidDataException($"Dictionary '{target.FullName}' has no Add method.");
            }

            Type pk = add.GetParameters()[0].ParameterType;
            Type pv = add.GetParameters()[1].ParameterType;
            for (int i = 0; i < count; i++)
            {
                object? k = ConvertForCollection(ReadValue(reader, dictTypes.kType), pk);
                object? v = ConvertForCollection(ReadValue(reader, dictTypes.vType), pv);
                add.Invoke(instance, new[] { k, v });
            }

            return instance;
        }

        private sealed class MemberAccessor
        {
            public string Name { get; set; } = string.Empty;
            public Type MemberType { get; set; } = typeof(object);
            public PropertyInfo? Prop { get; set; }
            public FieldInfo? Field { get; set; }
            public int Order { get; set; }
            public bool Required { get; set; }
            public bool CanWrite { get; set; }
            public Func<object, object?>? Getter { get; set; }
            public Action<object, object?>? Setter { get; set; }
        }

        private sealed class TypePlan
        {
            public MemberAccessor[] Members { get; set; } = Array.Empty<MemberAccessor>();
            public Dictionary<string, MemberAccessor> ByName { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public ConstructorInfo? Ctor { get; set; }
            public MemberAccessor?[] CtorMembers { get; set; } = Array.Empty<MemberAccessor?>();
        }

        private static readonly ConcurrentDictionary<Type, TypePlan> PlanCache = new();

        private static void WriteObjectPayload(BinaryWriter writer, object value, Type actual)
        {
            TypePlan plan = GetPlan(actual);
            writer.Write(plan.Members.Length);
            foreach (MemberAccessor m in plan.Members)
            {
                writer.Write(m.Name);
                object? v;
                try
                {
                    v = m.Getter!(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Cannot read '{actual.FullName}.{m.Name}'.", ex);
                }

                WriteValue(writer, v, m.MemberType);
            }
        }

        private static object ReadObject(BinaryReader reader, Type target)
        {
            if (target.IsInterface || target.IsAbstract)
                throw new InvalidDataException($"Cannot create abstract '{target.FullName}' without runtime type info.");

            if (IsWellKnownStruct(target))
                throw new InvalidDataException($"Unsupported object type '{target.FullName}'.");

            TypePlan plan = GetPlan(target);
            int propCount = reader.ReadInt32();
            CheckCount(propCount);

            Dictionary<string, object?> values = new(propCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < propCount; i++)
            {
                string name = reader.ReadString();
                if (plan.ByName.TryGetValue(name, out MemberAccessor? m))
                {
                    object? v = ReadValue(reader, m.MemberType);
                    values[m.Name] = v;
                }
                else
                    SkipValue(reader);
            }

            foreach (MemberAccessor m in plan.Members)
            {
                if (m.Required && !values.ContainsKey(m.Name))
                    throw new InvalidDataException($"Missing required '{target.FullName}.{m.Name}'.");
            }

            object instance;
            if (plan.Ctor is null || plan.Ctor.GetParameters().Length == 0)
            {
                try
                {
                    instance = Activator.CreateInstance(target, nonPublic: true)!;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Cannot create '{target.FullName}'. Add a parameterless constructor or [BinIgnore] unsupported members.", ex);
                }

                foreach ((string key, object? val) in values)
                {
                    MemberAccessor m = plan.ByName[key];
                    if (!m.CanWrite || m.Setter is null)
                        continue;

                    m.Setter(instance, Coerce(val, m.MemberType));
                }

                return instance;
            }

            ParameterInfo[] ps = plan.Ctor.GetParameters();
            object?[] args = new object?[ps.Length];
            HashSet<string> ctorNames = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ps.Length; i++)
            {
                MemberAccessor? mm = plan.CtorMembers[i];
                if (mm is not null)
                    ctorNames.Add(mm.Name);

                if (mm is not null && values.TryGetValue(mm.Name, out object? v))
                {
                    args[i] = Coerce(v, ps[i].ParameterType);
                }
                else if (ps[i].HasDefaultValue)
                {
                    args[i] = ps[i].DefaultValue;
                }
                else if (!ps[i].ParameterType.IsValueType || Nullable.GetUnderlyingType(ps[i].ParameterType) is not null)
                {
                    args[i] = null;
                }
                else
                    args[i] = Activator.CreateInstance(ps[i].ParameterType);
            }

            try
            {
                instance = plan.Ctor.Invoke(args)!;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Cannot construct '{target.FullName}'.", ex);
            }

            foreach ((string key, object? val) in values)
            {
                MemberAccessor m = plan.ByName[key];
                if (ctorNames.Contains(key) && IsCtorOnly(m))
                    continue;

                if (!m.CanWrite || m.Setter is null)
                    continue;

                m.Setter(instance, Coerce(val, m.MemberType));
            }

            return instance;
        }

        private static bool IsCtorOnly(MemberAccessor m) =>
            !HasUsableSetter(m.Prop, m.Field);

        private static void PopulateInto(BinaryReader reader, object instance, Type target)
        {
            BinKind kind;
            try
            {
                kind = (BinKind)reader.ReadByte();
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Truncated bin file.", ex);
            }

            if (kind == BinKind.Null)
                throw new InvalidDataException("Null payload for existing instance.");

            if (kind == BinKind.RuntimeTyped)
            {
                string aqn = reader.ReadString();
                Type? rt = BinaryTypeNames.Resolve(aqn) ?? throw new InvalidDataException($"Unknown runtime type '{aqn}'.");
                if (!target.IsAssignableFrom(rt))
                    throw new InvalidDataException($"Runtime type '{rt.FullName}' is not '{target.FullName}'.");

                PopulateInto(reader, instance, rt);
                return;
            }

            if (kind != BinKind.Object)
                throw new InvalidDataException($"Expected object payload for '{target.FullName}' but got {kind}.");

            TypePlan plan = GetPlan(target);
            int propCount = reader.ReadInt32();
            CheckCount(propCount);
            for (int i = 0; i < propCount; i++)
            {
                string name = reader.ReadString();
                if (plan.ByName.TryGetValue(name, out MemberAccessor? m))
                {
                    object? v = ReadValue(reader, m.MemberType);
                    if (m.CanWrite && m.Setter is not null)
                        m.Setter(instance, Coerce(v, m.MemberType));
                }
                else
                    SkipValue(reader);
            }
        }

        private static object? Coerce(object? value, Type dest)
        {
            if (value is null)
                return dest.IsValueType && Nullable.GetUnderlyingType(dest) is null ? Activator.CreateInstance(dest) : null;

            if (dest.IsAssignableFrom(value.GetType()))
                return value;

            Type t = Nullable.GetUnderlyingType(dest) ?? dest;
            if (t.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(t, s, ignoreCase: true);

                return Enum.ToObject(t, value);
            }

            return Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
        }

        private static void SkipValue(BinaryReader reader)
        {
            BinKind kind;
            try
            {
                kind = (BinKind)reader.ReadByte();
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Truncated bin file while skipping.", ex);
            }

            switch (kind)
            {
                case BinKind.Null:
                    return;

                case BinKind.Bool:
                    reader.ReadBoolean();
                    return;

                case BinKind.Byte:
                    reader.ReadByte();
                    return;

                case BinKind.SByte:
                    reader.ReadSByte();
                    return;

                case BinKind.Int16:
                    reader.ReadInt16();
                    return;

                case BinKind.UInt16:
                    reader.ReadUInt16();
                    return;

                case BinKind.Int32:
                    reader.ReadInt32();
                    return;

                case BinKind.UInt32:
                    reader.ReadUInt32();
                    return;

                case BinKind.Int64:
                    reader.ReadInt64();
                    return;

                case BinKind.UInt64:
                    reader.ReadUInt64();
                    return;

                case BinKind.Float:
                    reader.ReadSingle();
                    return;

                case BinKind.Double:
                    reader.ReadDouble();
                    return;

                case BinKind.Decimal:
                    reader.ReadDecimal();
                    return;

                case BinKind.Char:
                    reader.ReadChar();
                    return;

                case BinKind.Uri or BinKind.String:
                    reader.ReadString();
                    return;

                case BinKind.DateTime:
                    reader.ReadInt64();
                    reader.ReadByte();
                    return;

                case BinKind.TimeSpan:
                    reader.ReadInt64();
                    return;

                case BinKind.Guid:
                    ReadBytesExact(reader, 16);
                    return;

                case BinKind.DateTimeOffset:
                    reader.ReadInt64();
                    reader.ReadInt16();
                    return;

                case BinKind.ByteArray:
                    int len = reader.ReadInt32();
                    CheckCount(len);
                    ReadBytesExact(reader, len);
                    return;
                    
                case BinKind.RuntimeTyped:
                    reader.ReadString();
                    SkipValue(reader);
                    return;
                    
                case BinKind.List:
                    {
                        int n = reader.ReadInt32();
                        CheckCount(n);
                        for (int i = 0; i < n; i++)
                            SkipValue(reader);

                        return;
                    }

                case BinKind.Dictionary:
                    {
                        int n = reader.ReadInt32();
                        CheckCount(n);
                        for (int i = 0; i < n; i++)
                        {
                            SkipValue(reader);
                            SkipValue(reader);
                        }

                        return;
                    }

                case BinKind.Object:
                    {
                        int n = reader.ReadInt32();
                        CheckCount(n);
                        for (int i = 0; i < n; i++)
                        {
                            reader.ReadString();
                            SkipValue(reader);
                        }

                        return;
                    }

                default:
                    throw new InvalidDataException($"Unknown bin kind tag {(byte)kind}.");
            }
        }

        private static TypePlan GetPlan(Type type) => PlanCache.GetOrAdd(type, BuildPlan);

        private static TypePlan BuildPlan(Type type)
        {
            if (IsPrimitiveLike(type))
                throw new InvalidDataException($"'{type.FullName}' is a primitive and has no object plan.");

            List<MemberAccessor> members = new();

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                if (prop.GetCustomAttribute<BinaryIgnoreAttribute>() is not null)
                    continue;

                if (HasAttributeNamed(prop, "YamlIgnoreAttribute") || HasAttributeNamed(prop, "JsonIgnoreAttribute"))
                    continue;

                MethodInfo? getter = prop.GetGetMethod(nonPublic: false);
                if (getter is null)
                    continue;

                BinaryPropertyAttribute? attr = prop.GetCustomAttribute<BinaryPropertyAttribute>();
                string name = attr?.Name ?? prop.Name;
                if (string.IsNullOrWhiteSpace(name))
                    name = prop.Name;

                MethodInfo? setter = prop.GetSetMethod(nonPublic: true);
                FieldInfo? backing = null;
                bool canWrite = setter is not null;
                if (!canWrite)
                {
                    backing = type.GetField($"<{prop.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (backing is not null)
                        canWrite = true;
                }

                Func<object, object?> get = prop.GetValue;
                Action<object, object?>? set = null;
                if (setter is not null)
                {
                    MethodInfo s = setter;
                    set = (obj, val) => s.Invoke(obj, new[] { val });
                }
                else if (backing is not null)
                {
                    FieldInfo f = backing;
                    set = (obj, val) => f.SetValue(obj, val);
                }

                members.Add(new MemberAccessor
                {
                    Name = name,
                    MemberType = prop.PropertyType,
                    Prop = prop,
                    Field = null,
                    Order = attr?.Order ?? 0,
                    Required = attr?.Required ?? false,
                    CanWrite = canWrite,
                    Getter = get,
                    Setter = set,
                });
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<BinaryIgnoreAttribute>() is not null)
                    continue;

                if (HasAttributeNamed(field, "YamlIgnoreAttribute") || HasAttributeNamed(field, "JsonIgnoreAttribute"))
                    continue;

                BinaryPropertyAttribute? attr = field.GetCustomAttribute<BinaryPropertyAttribute>();
                string name = attr?.Name ?? field.Name;
                FieldInfo f = field;
                members.Add(new MemberAccessor
                {
                    Name = name,
                    MemberType = field.FieldType,
                    Prop = null,
                    Field = field,
                    Order = attr?.Order ?? 0,
                    Required = attr?.Required ?? false,
                    CanWrite = !field.IsInitOnly,
                    Getter = obj => f.GetValue(obj),
                    Setter = field.IsInitOnly ? null : (obj, val) => f.SetValue(obj, val),
                });
            }

            members.Sort((a, b) =>
            {
                int c = a.Order.CompareTo(b.Order);
                return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ConstructorInfo? chosen = null;
            MemberAccessor?[] chosenMap = Array.Empty<MemberAccessor?>();

            if (type.IsValueType)
            {
                ConstructorInfo? best = null;
                MemberAccessor?[] bestMap = Array.Empty<MemberAccessor?>();
                int bestScore = -1;
                foreach (ConstructorInfo c in ctors)
                {
                    ParameterInfo[] ps = c.GetParameters();
                    if (ps.Length == 0)
                        continue;

                    MemberAccessor?[] map = new MemberAccessor?[ps.Length];
                    bool allMatch = true;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        MemberAccessor? m = FindMember(members, ps[i].Name ?? string.Empty);
                        if (m is null)
                        {
                            allMatch = false;
                            break;
                        }

                        map[i] = m;
                    }
                    
                    if (allMatch && ps.Length > bestScore)
                    {
                        bestScore = ps.Length;
                        best = c;
                        bestMap = map;
                    }
                }

                bool hasGetOnly = members.Any(m => !m.CanWrite);
                if (best is not null && (hasGetOnly || bestScore > 0))
                {
                    chosen = best;
                    chosenMap = bestMap;
                }
            }
            else
            {
                chosen = ctors.FirstOrDefault(c => c.GetParameters().Length == 0);
                if (chosen is not null)
                {
                    chosenMap = Array.Empty<MemberAccessor?>();
                }
                else
                {
                    ConstructorInfo? best = null;
                    MemberAccessor?[] bestMap = Array.Empty<MemberAccessor?>();
                    int bestScore = -1;
                    foreach (ConstructorInfo c in ctors)
                    {
                        ParameterInfo[] ps = c.GetParameters();
                        MemberAccessor?[] map = new MemberAccessor?[ps.Length];
                        bool allMatch = true;
                        for (int i = 0; i < ps.Length; i++)
                        {
                            MemberAccessor? m = FindMember(members, ps[i].Name ?? string.Empty);
                            if (m is null)
                            {
                                allMatch = false;
                                break;
                            }

                            map[i] = m;
                        }

                        if (allMatch && ps.Length > bestScore)
                        {
                            bestScore = ps.Length;
                            best = c;
                            bestMap = map;
                        }
                    }

                    if (best is not null)
                    {
                        chosen = best;
                        chosenMap = bestMap;
                    }
                }
            }

            HashSet<string> coveredByCtor = new(StringComparer.OrdinalIgnoreCase);
            if (chosen is not null)
            {
                foreach (MemberAccessor? m in chosenMap)
                {
                    if (m is not null)
                        coveredByCtor.Add(m.Name);
                }
            }

            members.RemoveAll(m => !m.CanWrite && !coveredByCtor.Contains(m.Name));

            if (!type.IsValueType && chosen is null && ctors.Length > 0)
                throw new InvalidDataException($"'{type.FullName}' has no parameterless constructor and no constructor matching its properties. Add a parameterless constructor or [BinIgnore] unsupported members.");

            if (!type.IsValueType && chosen is null && ctors.Length == 0)
                chosen = null;

            Dictionary<string, MemberAccessor> byName = new(StringComparer.OrdinalIgnoreCase);
            foreach (MemberAccessor m in members)
            {
                byName.TryAdd(m.Name, m);
                if (m.Prop is not null)
                {
                    byName.TryAdd(m.Prop.Name, m);
                }
                else if (m.Field is not null)
                    byName.TryAdd(m.Field.Name, m);
            }

            return new TypePlan
            {
                Members = members.ToArray(),
                ByName = byName,
                Ctor = chosen,
                CtorMembers = chosenMap,
            };
        }

        private static MemberAccessor? FindMember(List<MemberAccessor> members, string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramName))
                return null;

            foreach (MemberAccessor m in members)
            {
                string clr = m.Prop?.Name ?? m.Field?.Name ?? m.Name;
                if (string.Equals(clr, paramName, StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, paramName, StringComparison.OrdinalIgnoreCase))
                    return m;
            }
            return null;
        }

        private static bool HasUsableSetter(PropertyInfo? prop, FieldInfo? field)
        {
            if (prop?.GetSetMethod(nonPublic: true) is not null)
                return true;

            return field is not null;
        }

        private static bool HasAttributeNamed(MemberInfo member, string attributeTypeName)
        {
            foreach (Attribute a in member.GetCustomAttributes())
            {
                if (a.GetType().Name == attributeTypeName)
                    return true;
            }

            return false;
        }

        private static bool IsNonNullableValueType(Type t)
            => t.IsValueType && Nullable.GetUnderlyingType(t) is null;

        private static bool IsPrimitiveLike(Type t)
        {
            Type u = Nullable.GetUnderlyingType(t) ?? t;
            if (u.IsEnum)
                return true;

            return u == typeof(bool) || u == typeof(byte) || u == typeof(sbyte) || u == typeof(short) || u == typeof(ushort) || u == typeof(int) || u == typeof(uint) || u == typeof(long) || u == typeof(ulong) || u == typeof(float) || u == typeof(double) || u == typeof(decimal) || u == typeof(char) || u == typeof(string) || u == typeof(DateTime) || u == typeof(TimeSpan) || u == typeof(Guid) || u == typeof(DateTimeOffset) || u == typeof(Uri) || u == typeof(byte[]) || u == typeof(Version);
        }

        private static bool IsWellKnownStruct(Type t)
            => t == typeof(DateTime) || t == typeof(TimeSpan) || t == typeof(Guid) || t == typeof(DateTimeOffset) || t == typeof(decimal);

        private static bool TryGetDictionaryTypes(Type type, out Type? keyType, out Type? valueType)
        {
            keyType = null;
            valueType = null;
            if (type == typeof(string))
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                return false;

            if (type.IsArray)
                return false;

            if (type.IsGenericType)
            {
                Type def = type.GetGenericTypeDefinition();
                if (def == typeof(Dictionary<,>) || def == typeof(SortedDictionary<,>) || def == typeof(SortedList<,>))
                {
                    Type[] args = type.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    return true;
                }
            }

            Type? iface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ?? type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
            if (iface is not null)
            {
                Type[] args = iface.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                keyType = typeof(object);
                valueType = typeof(object);
                return true;
            }

            return false;
        }

        private static bool TryGetEnumerableElementType(Type type, out Type? elementType)
        {
            elementType = null;
            if (type == typeof(string) || type == typeof(byte[]))
                return false;

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    return false;

                elementType = type.GetElementType();
                return elementType is not null;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                return false;

            if (type.IsGenericType)
            {
                Type def = type.GetGenericTypeDefinition();
                if (def == typeof(List<>) || def == typeof(HashSet<>) || def == typeof(Queue<>) || def == typeof(Stack<>) || def == typeof(LinkedList<>) || def == typeof(SortedSet<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            Type? iface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (iface is not null)
            {
                elementType = iface.GetGenericArguments()[0];
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        private static void CheckCount(int count)
        {
            if (count < 0 || count > MaxEntries)
                throw new InvalidDataException($"Invalid bin collection count: {count}.");
        }

        private static byte[] ReadBytesExact(BinaryReader reader, int count)
        {
            if (count < 0)
                throw new InvalidDataException($"Invalid byte length: {count}.");

            byte[] buf = reader.ReadBytes(count);
            if (buf.Length != count)
                throw new InvalidDataException($"Truncated bin file (expected {count} bytes, got {buf.Length}).");

            return buf;
        }
    }
}
#endif