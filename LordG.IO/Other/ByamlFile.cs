﻿using System.Collections;
using Syroot.Maths;

namespace LordG.IO.Other
{
    public struct BymlFileData
    {
        public ByteOrder byteOrder;
        public ushort Version;
        public bool SupportPaths;
        public dynamic RootNode;

        public override string ToString()
        {
            return YamlByamlConverter.ToYaml(this);
        }

        public static implicit operator BymlFileData(YmlFile file) 
            => YamlByamlConverter.FromYaml(file.ReadAllText());
    }

    /// <summary>
    /// Represents the loading and saving logic of BYAML files and returns the resulting file structure in dynamic
    /// objects.
    /// </summary>
	/// 
    public class ByamlFile
    {
        // ---- CONSTANTS ----------------------------------------------------------------------------------------------

        private const ushort _magicBytes = 0x4259; // "BY"
                                                   // ---- MEMBERS ------------------------------------------------------------------------------------------------
        private ushort _version;
        private bool _supportPaths;
        private ByteOrder _byteOrder;

        private List<string> _nameArray;
        private List<string> _stringArray;
        private List<List<ByamlPathPoint>> _pathArray;
        private bool _fastLoad = false;

        // ---- CONSTRUCTORS & DESTRUCTOR ------------------------------------------------------------------------------

        private ByamlFile(bool supportPaths, ByteOrder byteOrder, ushort _ver, bool fastLoad = false)
        {
            _version = _ver;
            _supportPaths = supportPaths;
            _byteOrder = byteOrder;
            _fastLoad = fastLoad;
            if (fastLoad)
            {
                AlreadyReadNodes = new Dictionary<uint, dynamic>();
            }
        }

        // ---- METHODS (PUBLIC) ---------------------------------------------------------------------------------------

        /// <summary>
        /// Deserializes and returns the dynamic value of the BYAML node read from the given file.
        /// </summary>
        /// <param name="fileName">The name of the file to read the data from.</param>
        /// <param name="supportPaths">Whether to expect a path array offset. This must be enabled for Mario Kart 8
        /// files.</param>
        /// <param name="byteOrder">The <see cref="ByteOrder"/> to read data in.</param>
        public static BymlFileData LoadN(string fileName, bool supportPaths = false, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            using FileStream stream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            return LoadN(stream, supportPaths, byteOrder);
        }

        /// <summary>
        /// Deserializes and returns the dynamic value of the BYAML node read from the specified stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read the data from.</param>
        /// <param name="supportPaths">Whether to expect a path array offset. This must be enabled for Mario Kart 8
        /// files.</param>
        /// <param name="byteOrder">The <see cref="ByteOrder"/> to read data in.</param>
        public static BymlFileData LoadN(Stream stream, bool supportPaths = false, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            ByamlFile byamlFile = new(supportPaths, byteOrder, 3);
            var r = byamlFile.Read(stream);
            return new BymlFileData() { byteOrder = byamlFile._byteOrder, RootNode = r, Version = byamlFile._version, SupportPaths = supportPaths };
        }

        /// <summary>
        /// Deserializes and returns the dynamic value of the BYAML node read from the specified stream keeping the references, do not use this if you intend to edit the byml.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read the data from.</param>
        /// <param name="supportPaths">Whether to expect a path array offset. This must be enabled for Mario Kart 8
        /// files.</param>
        /// <param name="byteOrder">The <see cref="ByteOrder"/> to read data in.</param>
        public static BymlFileData FastLoadN(Stream stream, bool supportPaths = false, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            ByamlFile byamlFile = new(supportPaths, byteOrder, 3, true);
            var r = byamlFile.Read(stream);
            return new BymlFileData() { byteOrder = byamlFile._byteOrder, RootNode = r, Version = byamlFile._version, SupportPaths = supportPaths };
        }

        /// <summary>
        /// Serializes the given dynamic value which requires to be an array or dictionary of BYAML compatible values
        /// and stores it in the given file.
        /// </summary>
        public static void SaveN(string fileName, BymlFileData file)
        {
            using FileStream stream = new(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            SaveN(stream, file);
        }

        public static byte[] SaveN(BymlFileData file)
        {
            using MemoryStream stream = new();
            SaveN(stream, file);
            return stream.ToArray();
        }

        /// <summary>
        /// Serializes the given dynamic value which requires to be an array or dictionary of BYAML compatible values
        /// and stores it in the specified stream.
        /// </summary>
        public static void SaveN(Stream stream, BymlFileData file)
        {
            ByamlFile byamlFile = new(file.SupportPaths, file.byteOrder, file.Version);
            byamlFile.Write(stream, file.RootNode);
        }

        // ---- Helper methods ----

        /// <summary>
        /// Tries to retrieve the value of the element with the specified <paramref name="key"/> stored in the given
        /// dictionary <paramref name="node"/>. If the key does not exist, <c>null</c> is returned.
        /// </summary>
        /// <param name="node">The dictionary BYAML node to retrieve the value from.</param>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value stored under the given key or <c>null</c> if the key is not present.</returns>
        public static dynamic GetValue(IDictionary<string, dynamic> node, string key)
        {
            dynamic value;
            return node.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>
        /// Sets the given <paramref name="value"/> in the provided dictionary <paramref name="node"/> under the
        /// specified <paramref name="key"/>. If the value is <c>null</c>, the key is removed from the dictionary node.
        /// </summary>
        /// <param name="node">The dictionary node to store the value under.</param>
        /// <param name="key">The key under which the value will be stored or which will be removed.</param>
        /// <param name="value">The value to store under the key or <c>null</c> to remove the key.</param>
        public static void SetValue(IDictionary<string, dynamic> node, string key, dynamic value)
        {
            if (value == null)
            {
                node.Remove(key);
            }
            else
            {
                node[key] = value;
            }
        }

        /// <summary>
        /// Casts all elements of the given array <paramref name="node"/> into the provided type
        /// <typeparamref name="T"/>. If the node is <c>null</c>, <c>null</c> is returned.
        /// </summary>
        /// <typeparam name="T">The type to cast each element to.</typeparam>
        /// <param name="node">The array node which elements will be casted.</param>
        /// <returns>The list of type <typeparamref name="T"/> or <c>null</c> if the node is <c>null</c>.</returns>
        public static List<T> GetList<T>(IEnumerable<dynamic> node)
        {
            return node?.Cast<T>().ToList();
        }

        // ---- METHODS (PRIVATE) --------------------------------------------------------------------------------------

        // ---- Loading ----
        private dynamic Read(Stream stream)
        {
            // Open a reader on the given stream.
            using BinaryDataReader reader = new(stream, Encoding.UTF8, true);
            reader.ByteOrder = _byteOrder;

            // Load the header, specifying magic bytes ("BY"), version and main node offsets.
            if (reader.ReadUInt16() != _magicBytes)
            {
                _byteOrder = _byteOrder == ByteOrder.LittleEndian ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
                reader.ByteOrder = _byteOrder;
                reader.BaseStream.Position = 0;
                if (reader.ReadUInt16() != _magicBytes) throw new Exception("Header mismatch");
            }
            _version = reader.ReadUInt16();
            uint nameArrayOffset = reader.ReadUInt32();
            uint stringArrayOffset = reader.ReadUInt32();

            using (reader.TemporarySeek())
            {
                // Paths are supported if the third offset is a path array (or null) and the fourth a root.
                ByamlNodeType thirdNodeType = PeekNodeType(reader);
                reader.Seek(sizeof(uint));
                ByamlNodeType fourthNodeType = PeekNodeType(reader);

                _supportPaths = (thirdNodeType == ByamlNodeType.None || thirdNodeType == ByamlNodeType.PathArray)
                     && (fourthNodeType == ByamlNodeType.Array || fourthNodeType == ByamlNodeType.Dictionary);

            }


            uint pathArrayOffset = 0;
            if (_supportPaths)
            {
                pathArrayOffset = reader.ReadUInt32();
            }
            uint rootNodeOffset = reader.ReadUInt32();

            // Read the name array, holding strings referenced by index for the names of other nodes.
            if (nameArrayOffset != 0)
            {
                reader.Seek(nameArrayOffset, SeekOrigin.Begin);
                _nameArray = ReadNode(reader);
            }

            // Read the optional string array, holding strings referenced by index in string nodes.
            if (stringArrayOffset != 0)
            {
                reader.Seek(stringArrayOffset, SeekOrigin.Begin);
                _stringArray = ReadNode(reader);
            }

            // Read the optional path array, holding paths referenced by index in path nodes.
            if (_supportPaths && pathArrayOffset != 0)
            {
                // The third offset is the root node, so just read that and we're done.
                reader.Seek(pathArrayOffset, SeekOrigin.Begin);
                _pathArray = ReadNode(reader);
            }

            if (rootNodeOffset == 0) //empty byml
            {
                return new List<dynamic>();
            }

            // Read the root node.
            reader.Seek(rootNodeOffset, SeekOrigin.Begin);
            return ReadNode(reader, 0);
        }

        private static ByamlNodeType PeekNodeType(BinaryDataReader reader)
        {
            using (reader.TemporarySeek())
            {
                // If the offset is invalid, the type cannot be determined.
                uint offset = reader.ReadUInt32();
                if (offset > 0 && offset < reader.BaseStream.Length)
                {
                    // Seek to the offset and try to read a valid type.
                    reader.Position = offset;
                    ByamlNodeType nodeType = (ByamlNodeType)reader.ReadByte();
                    if (Enum.IsDefined(typeof(ByamlNodeType), nodeType))
                        return nodeType;
                }
            }
            return ByamlNodeType.None;

        }

        //Node references are disabled unless fastLoad is active since it leads to multiple objects sharing the same values for different fields (eg. a position node can be shared between multiple objects)
        private Dictionary<uint, dynamic> AlreadyReadNodes = null; //Offset in the file, reference to node

        private dynamic ReadNode(BinaryDataReader reader, ByamlNodeType nodeType = 0)
        {
            // Read the node type if it has not been provided yet.
            bool nodeTypeGiven = nodeType != 0;
            if (!nodeTypeGiven)
            {
                nodeType = (ByamlNodeType)reader.ReadByte();
            }
            if (nodeType >= ByamlNodeType.Array && nodeType <= ByamlNodeType.PathArray)
            {
                // Get the length of arrays.
                long? oldPos = null;
                uint offset = 0;
                if (nodeTypeGiven)
                {
                    // If the node type was given, the array value is read from an offset.
                    offset = reader.ReadUInt32();

                    if (_fastLoad && AlreadyReadNodes.ContainsKey(offset))
                    {
                        return AlreadyReadNodes[offset];
                    }

                    oldPos = reader.Position;
                    reader.Seek(offset, SeekOrigin.Begin);
                }
                else
                {
                    reader.Seek(-1);
                }
                dynamic value = null;
                int length = (int)Get3LsbBytes(reader.ReadUInt32());
                value = nodeType switch
                {
                    ByamlNodeType.Array => ReadArrayNode(reader, length, offset),
                    ByamlNodeType.Dictionary => ReadDictionaryNode(reader, length, offset),
                    ByamlNodeType.StringArray => ReadStringArrayNode(reader, length),
                    ByamlNodeType.PathArray => ReadPathArrayNode(reader, length),
                    _ => throw new ByamlException($"Unknown node type '{nodeType}'."),
                };
                // Seek back to the previous position if this was a value positioned at an offset.
                if (oldPos.HasValue)
                {
                    reader.Seek(oldPos.Value, SeekOrigin.Begin);
                }
                return value;
            }
            else
            {
                // Read the following UInt32 which is representing the value directly.
                switch (nodeType)
                {
                    case ByamlNodeType.StringIndex:
                        return _stringArray[reader.ReadInt32()];
                    case ByamlNodeType.PathIndex:
                        return _pathArray[reader.ReadInt32()];
                    case ByamlNodeType.Boolean:
                        return reader.ReadInt32() != 0;
                    case ByamlNodeType.Integer:
                        return reader.ReadInt32();
                    case ByamlNodeType.Float:
                        return reader.ReadSingle();
                    case ByamlNodeType.Uinteger:
                        return reader.ReadUInt32();
                    case ByamlNodeType.Long:
                    case ByamlNodeType.ULong:
                    case ByamlNodeType.Double:
                        var pos = reader.Position;
                        reader.Position = reader.ReadUInt32();
                        dynamic value = readLongValFromOffset(nodeType);
                        reader.Position = pos + 4;
                        return value;
                    case ByamlNodeType.Null:
                        reader.Seek(0x4);
                        return null;
                    default:
                        throw new ByamlException($"Unknown node type '{nodeType}'.");
                }
            }

            dynamic readLongValFromOffset(ByamlNodeType type)
            {
                return type switch
                {
                    ByamlNodeType.Long => reader.ReadInt64(),
                    ByamlNodeType.ULong => reader.ReadUInt64(),
                    ByamlNodeType.Double => (dynamic)reader.ReadDouble(),
                    _ => throw new ByamlException($"Unknown node type '{nodeType}'."),
                };
            }
        }

        private List<dynamic> ReadArrayNode(BinaryDataReader reader, int length, uint offset = 0)
        {
            List<dynamic> array = new(length);

            if (_fastLoad && offset != 0) AlreadyReadNodes.Add(offset, array);

            // Read the element types of the array.
            byte[] nodeTypes = reader.ReadBytes(length);
            // Read the elements, which begin after a padding to the next 4 bytes.
            reader.Align(4);
            for (int i = 0; i < length; i++)
            {
                array.Add(ReadNode(reader, (ByamlNodeType)nodeTypes[i]));
            }

            return array;
        }

        private Dictionary<string, dynamic> ReadDictionaryNode(BinaryDataReader reader, int length, uint offset = 0)
        {
            Dictionary<string, dynamic> dictionary = new();

            if (_fastLoad && offset != 0) AlreadyReadNodes.Add(offset, dictionary);

            // Read the elements of the dictionary.
            for (int i = 0; i < length; i++)
            {
                uint indexAndType = reader.ReadUInt32();
                int nodeNameIndex = (int)Get3MsbBytes(indexAndType);
                ByamlNodeType nodeType = (ByamlNodeType)Get1MsbByte(indexAndType);
                string nodeName = _nameArray[nodeNameIndex];
                dictionary.Add(nodeName, ReadNode(reader, nodeType));
            }

            return dictionary;
        }

        private List<string> ReadStringArrayNode(BinaryDataReader reader, int length)
        {
            List<string> stringArray = new(length);

            // Read the element offsets.
            long nodeOffset = reader.Position - 4; // String offsets are relative to the start of node.
            uint[] offsets = reader.ReadUInt32s(length);

            // Read the strings by seeking to their element offset and then back.
            long oldPosition = reader.Position;
            for (int i = 0; i < length; i++)
            {
                reader.Seek(nodeOffset + offsets[i], SeekOrigin.Begin);
                stringArray.Add(reader.ReadString(BinaryStringFormat.ZeroTerminated));
            }
            reader.Seek(oldPosition, SeekOrigin.Begin);

            return stringArray;
        }

        private List<List<ByamlPathPoint>> ReadPathArrayNode(BinaryDataReader reader, int length)
        {
            List<List<ByamlPathPoint>> pathArray = new(length);

            // Read the element offsets.
            long nodeOffset = reader.Position - 4; // Path offsets are relative to the start of node.
            uint[] offsets = reader.ReadUInt32s(length + 1);

            // Read the paths by seeking to their element offset and then back.
            long oldPosition = reader.Position;
            for (int i = 0; i < length; i++)
            {
                reader.Seek(nodeOffset + offsets[i], SeekOrigin.Begin);
                int pointCount = (int)((offsets[i + 1] - offsets[i]) / 0x1C);
                pathArray.Add(ReadPath(reader, pointCount));
            }
            reader.Seek(oldPosition, SeekOrigin.Begin);

            return pathArray;
        }

        private List<ByamlPathPoint> ReadPath(BinaryDataReader reader, int length)
        {
            List<ByamlPathPoint> byamlPath = new();
            for (int j = 0; j < length; j++)
            {
                byamlPath.Add(ReadPathPoint(reader));
            }
            return byamlPath;
        }

        private ByamlPathPoint ReadPathPoint(BinaryDataReader reader)
        {
            ByamlPathPoint point = new();
            point.Position = new Vector3F(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            point.Normal = new Vector3F(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            point.Unknown = reader.ReadUInt32();
            return point;
        }

        // ---- Saving ----

        private void Write(Stream stream, object root)
        {
            // Check if the root is of the correct type.
            if (root == null)
            {
                throw new ByamlException("Root node must not be null.");
            }
            else if (!(root is IDictionary<string, dynamic> || root is IEnumerable))
            {
                throw new ByamlException($"Type '{root.GetType()}' is not supported as a BYAML root node.");
            }

            // Generate the name, string and path array nodes.
            _nameArray = new List<string>();
            _stringArray = new List<string>();
            _pathArray = new List<List<ByamlPathPoint>>();
            List<dynamic> tmp = new();
            CollectNodeArrayContents(root, ref tmp);
            tmp.Clear();
            _nameArray.Sort(StringComparer.Ordinal);
            _stringArray.Sort(StringComparer.Ordinal);

            // Open a writer on the given stream.
            using BinaryDataWriter writer = new(stream, Encoding.UTF8, true);
            writer.ByteOrder = _byteOrder;

            // Write the header, specifying magic bytes, version and main node offsets.
            writer.Write(_magicBytes);
            writer.Write(_version);
            Offset nameArrayOffset = writer.ReserveOffset();
            Offset stringArrayOffset = writer.ReserveOffset();
            Offset pathArrayOffset = _supportPaths ? writer.ReserveOffset() : null;
            Offset rootOffset = writer.ReserveOffset();

            // Write the main nodes.
            WriteValueContents(writer, nameArrayOffset, ByamlNodeType.StringArray, _nameArray);
            if (_stringArray.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                WriteValueContents(writer, stringArrayOffset, ByamlNodeType.StringArray, _stringArray);
            }

            // Include a path array offset if requested.
            if (_supportPaths)
            {
                if (_pathArray.Count == 0)
                {
                    writer.Write(0);
                }
                else
                {
                    WriteValueContents(writer, pathArrayOffset, ByamlNodeType.PathArray, _pathArray);
                }
            }

            // Write the root node.
            WriteValueContents(writer, rootOffset, GetNodeType(root), root);
        }

        private void CollectNodeArrayContents(dynamic node, ref List<dynamic> alreadyCollected)
        {
            if (node == null) return;
            foreach (var o in alreadyCollected.Where(x => !IEnumerableCompare.TypeNotEqual(x.GetType(), node.GetType())))
            {
                if (node is string)
                {
                    if (o == node)
                        return;
                }
                else if (node is IEnumerable)
                {
                    if (IEnumerableCompare.IsEqual(node, o))
                        return;
                }
                else if (node == o)
                    return;
            }

            alreadyCollected.Add(node);
            if (node is string)
            {
                if (!_stringArray.Contains((string)node))
                {
                    _stringArray.Add((string)node);
                }
            }
            else if (node is List<ByamlPathPoint>)
            {
                _pathArray.Add((List<ByamlPathPoint>)node);
            }
            else if (node is IDictionary<string, dynamic>)
            {
                foreach (KeyValuePair<string, dynamic> entry in node)
                {
                    if (!_nameArray.Contains(entry.Key))
                    {
                        _nameArray.Add(entry.Key);
                    }
                    CollectNodeArrayContents(entry.Value, ref alreadyCollected);
                }
            }
            else if (node is IList<dynamic>)
            {
                foreach (dynamic childNode in node)
                {
                    CollectNodeArrayContents(childNode, ref alreadyCollected);
                }
            }
        }

        Dictionary<dynamic, uint> alreadyWrittenNodes = new();
        private Offset WriteValue(BinaryDataWriter writer, dynamic value)
        {
            // Only reserve and return an offset for the complex value contents, write simple values directly.
            ByamlNodeType type = GetNodeType(value);
            switch (type)
            {
                case ByamlNodeType.StringIndex:
                    WriteStringIndexNode(writer, value);
                    return null;
                case ByamlNodeType.PathIndex:
                    WritePathIndexNode(writer, value);
                    return null;
                case ByamlNodeType.Dictionary:
                case ByamlNodeType.Array:
                    foreach (var d in alreadyWrittenNodes.Keys.Where(x => x is IEnumerable && x.Count == value.Count))
                    {
                        if (IEnumerableCompare.IsEqual(d, value))
                        {
                            writer.Write(alreadyWrittenNodes[d]);
                            return null;
                        }
                    }
                    return writer.ReserveOffset();
                case ByamlNodeType.Boolean:
                    writer.Write(value ? 1 : 0);
                    return null;
                case ByamlNodeType.Integer:
                case ByamlNodeType.Float:
                case ByamlNodeType.Uinteger:
                    writer.Write(value);
                    return null;
                case ByamlNodeType.Double:
                case ByamlNodeType.ULong:
                case ByamlNodeType.Long:
                    return writer.ReserveOffset();
                case ByamlNodeType.Null:
                    writer.Write(0x0);
                    return null;
                default:
                    throw new ByamlException($"{type} not supported as value node.");
            }
        }

        private void WriteValueContents(BinaryDataWriter writer, Offset offset, ByamlNodeType type, dynamic value)
        {
            if (alreadyWrittenNodes.ContainsKey(value))
            {
                offset.Satisfy((int)alreadyWrittenNodes[value]);
                return;
            }
            else if (value is IEnumerable)
            {
                foreach (var d in alreadyWrittenNodes.Keys.Where(x => x is IEnumerable && x.Count == value.Count))
                {
                    if (IEnumerableCompare.IsEqual(d, value))
                    {
                        offset.Satisfy((int)alreadyWrittenNodes[d]);
                        return;
                    }
                }
            }

            // Satisfy the offset to the complex node value which must be 4-byte aligned.
            writer.Align(4);
            offset.Satisfy();

            // Write the value contents.
            switch (type)
            {
                case ByamlNodeType.Dictionary:
                    alreadyWrittenNodes.Add(value, (uint)writer.Position);
                    WriteDictionaryNode(writer, value);
                    break;
                case ByamlNodeType.StringArray:
                    WriteStringArrayNode(writer, value);
                    break;
                case ByamlNodeType.PathArray:
                    alreadyWrittenNodes.Add(value, (uint)writer.Position);
                    WritePathArrayNode(writer, value);
                    break;
                case ByamlNodeType.Array:
                    alreadyWrittenNodes.Add(value, (uint)writer.Position);
                    WriteArrayNode(writer, value);
                    break;
                case ByamlNodeType.Double:
                case ByamlNodeType.ULong:
                case ByamlNodeType.Long:
                    writer.Write(value);
                    break;
                default:
                    throw new ByamlException($"{type} not supported as complex node.");
            }
        }

        private void WriteTypeAndLength(BinaryDataWriter writer, ByamlNodeType type, dynamic node)
        {
            uint value;
            if (_byteOrder == ByteOrder.BigEndian)
            {
                value = (uint)type << 24 | (uint)Enumerable.Count(node);
            }
            else
            {
                value = (uint)type | (uint)Enumerable.Count(node) << 8;
            }
            writer.Write(value);
        }

        private void WriteStringIndexNode(BinaryDataWriter writer, string node)
        {
            uint index = (uint)_stringArray.IndexOf(node);
            writer.Write(index);
        }

        private void WritePathIndexNode(BinaryDataWriter writer, List<ByamlPathPoint> node)
        {
            writer.Write(_pathArray.IndexOf(node));
        }

        private void WriteArrayNode(BinaryDataWriter writer, IEnumerable node)
        {
            WriteTypeAndLength(writer, ByamlNodeType.Array, node);

            // Write the element types.
            foreach (dynamic element in node)
            {
                writer.Write((byte)GetNodeType(element));
            }

            // Write the elements, which begin after a padding to the next 4 bytes.
            writer.Align(4);
            Dictionary<Offset, dynamic> offsets = new();
            foreach (dynamic element in node)
            {
                var off = WriteValue(writer, element);
                if (off != null)
                    offsets.Add(off, element);
            }

            // Write the contents of complex nodes and satisfy the offsets.
            foreach (var element in offsets)
            {
                WriteValueContents(writer, element.Key, GetNodeType(element.Value), element.Value);
            }
        }

        private void WriteDictionaryNode(BinaryDataWriter writer, IDictionary<string, dynamic> node)
        {
            WriteTypeAndLength(writer, ByamlNodeType.Dictionary, node);

            // Dictionaries need to be sorted by key.
            var sortedDict = node.Values.Zip(node.Keys, (Value, Key) => new { Key, Value })
                .OrderBy(x => x.Key, StringComparer.Ordinal).ToList();

            // Write the key-value pairs.
            Dictionary<Offset, dynamic> offsets = new();
            foreach (var keyValuePair in sortedDict)
            {
                // Get the index of the key string in the file's name array and write it together with the type.
                uint keyIndex = (uint)_nameArray.IndexOf(keyValuePair.Key);
                if (_byteOrder == ByteOrder.BigEndian)
                {
                    writer.Write(keyIndex << 8 | (uint)GetNodeType(keyValuePair.Value));
                }
                else
                {
                    writer.Write(keyIndex | (uint)GetNodeType(keyValuePair.Value) << 24);
                }

                // Write the elements.
                var off = WriteValue(writer, keyValuePair.Value);
                if (off != null)
                    offsets.Add(off, keyValuePair.Value);
            }

            // Write the value contents.
            foreach (var element in offsets)
            {
                WriteValueContents(writer, element.Key, GetNodeType(element.Value), element.Value);
            }
        }

        private void WriteStringArrayNode(BinaryDataWriter writer, IEnumerable<string> node)
        {
            uint NodeStartPos = (uint)writer.BaseStream.Position;
            WriteTypeAndLength(writer, ByamlNodeType.StringArray, node);

            for (int i = 0; i <= node.Count(); i++) writer.Write(new byte[4]); //Space for offsets
            List<uint> offsets = new();
            foreach (string str in node)
            {
                offsets.Add((uint)writer.BaseStream.Position - NodeStartPos);
                writer.Write(str, BinaryStringFormat.ZeroTerminated);
            }
            offsets.Add((uint)writer.BaseStream.Position - NodeStartPos);
            writer.Align(4);
            uint backHere = (uint)writer.BaseStream.Position;
            writer.BaseStream.Position = NodeStartPos + 4;
            foreach (uint off in offsets) writer.Write(off);
            writer.BaseStream.Position = backHere;
        }


        private void WritePathArrayNode(BinaryDataWriter writer, IEnumerable<List<ByamlPathPoint>> node)
        {
            writer.Align(4);
            WriteTypeAndLength(writer, ByamlNodeType.PathArray, node);

            // Write the offsets to the paths, where the last one points to the end of the last path.
            long offset = 4 + 4 * (node.Count() + 1); // Relative to node start + all uint32 offsets.
            foreach (List<ByamlPathPoint> path in node)
            {
                writer.Write((uint)offset);
                offset += path.Count * 28; // 28 bytes are required for a single point.
            }
            writer.Write((uint)offset);

            // Write the paths.
            foreach (List<ByamlPathPoint> path in node)
            {
                WritePathNode(writer, path);
            }
        }

        private void WritePathNode(BinaryDataWriter writer, List<ByamlPathPoint> node)
        {
            foreach (ByamlPathPoint point in node)
            {
                WritePathPoint(writer, point);
            }
        }

        private void WritePathPoint(BinaryDataWriter writer, ByamlPathPoint point)
        {
            writer.Write(point.Position.X);
            writer.Write(point.Position.Y);
            writer.Write(point.Position.Z);
            writer.Write(point.Normal.X);
            writer.Write(point.Normal.Y);
            writer.Write(point.Normal.Z);
            writer.Write(point.Unknown);
        }

        // ---- Helper methods ----

        static internal ByamlNodeType GetNodeType(dynamic node, bool isInternalNode = false)
        {
            if (isInternalNode)
            {
                if (node is IEnumerable<string>) return ByamlNodeType.StringArray;
                else if (node is IEnumerable<List<ByamlPathPoint>>) return ByamlNodeType.PathArray;
                else throw new ByamlException($"Type '{node.GetType()}' is not supported as a main BYAML node.");
            }
            else
            {
                if (node is string)
                    return ByamlNodeType.StringIndex;
                else if (node is List<ByamlPathPoint>) return ByamlNodeType.PathIndex;
                else if (node is IDictionary<string, dynamic>) return ByamlNodeType.Dictionary;
                else if (node is IEnumerable) return ByamlNodeType.Array;
                else if (node is bool) return ByamlNodeType.Boolean;
                else if (node is int) return ByamlNodeType.Integer;
                else if (node is float) return ByamlNodeType.Float; /*TODO decimal is float or double ? */
                else if (node is uint) return ByamlNodeType.Uinteger;
                else if (node is Int64) return ByamlNodeType.Long;
                else if (node is UInt64) return ByamlNodeType.ULong;
                else if (node is double) return ByamlNodeType.Double;
                else if (node == null) return ByamlNodeType.Null;
                else throw new ByamlException($"Type '{node.GetType()}' is not supported as a BYAML node.");
            }
        }

        private uint Get1MsbByte(uint value)
        {
            if (_byteOrder == ByteOrder.BigEndian)
            {
                return value & 0x000000FF;
            }
            else
            {
                return value >> 24;
            }
        }

        private uint Get3LsbBytes(uint value)
        {
            if (_byteOrder == ByteOrder.BigEndian)
            {
                return value & 0x00FFFFFF;
            }
            else
            {
                return value >> 8;
            }
        }

        private uint Get3MsbBytes(uint value)
        {
            if (_byteOrder == ByteOrder.BigEndian)
            {
                return value >> 8;
            }
            else
            {
                return value & 0x00FFFFFF;
            }
        }
    }

    public static class IEnumerableCompare
    {

        private static bool IDictionaryIsEqual(IDictionary<string, dynamic> a, IDictionary<string, dynamic> b)
        {
            if (a.Count != b.Count) return false;
            foreach (string key in a.Keys)
            {
                if (!b.ContainsKey(key)) return false;
                if ((a[key] == null && b[key] != null) || (a[key] != null && b[key] == null)) return false;
                else if (a[key] == null && b[key] == null) continue;

                if (TypeNotEqual(a[key].GetType(), b[key].GetType())) return false;

                if (a[key] is IList<dynamic> && IListIsEqual(a[key], b[key])) continue;
                else if (a[key] is IDictionary<string, dynamic> && IDictionaryIsEqual(a[key], b[key])) continue;
                else if (a[key] == b[key]) continue;

                return false;
            }
            return true;
        }

        private static bool IListIsEqual(IList<dynamic> a, IList<dynamic> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if ((a[i] == null && b[i] != null) || (a[i] != null && b[i] == null)) return false;
                else if (a[i] == null && b[i] == null) continue;

                if (TypeNotEqual(a[i].GetType(), b[i].GetType())) return false;

                if (a[i] is IList<dynamic> && IListIsEqual(a[i], b[i])) continue;
                else if (a[i] is IDictionary<string, dynamic> && IDictionaryIsEqual(a[i], b[i])) continue;
                else if (a[i] == b[i]) continue;

                return false;
            }
            return true;
        }

        public static bool TypeNotEqual(Type a, Type b)
        {
            return !(a.IsAssignableFrom(b) || b.IsAssignableFrom(a)); // without this LinksNode wouldn't be equal to IDictionary<string,dynamic>
        }

        public static bool IsEqual(IEnumerable a, IEnumerable b)
        {
            if (TypeNotEqual(a.GetType(), b.GetType())) return false;
            if (a is IDictionary) return IDictionaryIsEqual((IDictionary<string, dynamic>)a, (IDictionary<string, dynamic>)b);
            else if (a is IList<ByamlPathPoint>) return ((IList<ByamlPathPoint>)a).SequenceEqual((IList<ByamlPathPoint>)b);
            else return IListIsEqual((IList<dynamic>)a, (IList<dynamic>)b);
        }
    }
}
