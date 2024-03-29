﻿namespace LordG.IO.Other
{
    /// <summary>
    /// Represents the type of which a dynamic BYAML node can be.
    /// </summary>
    internal enum ByamlNodeType : byte
	{
        /// <summary>
        /// Represents an empty type. Used to detect path nodes
        /// </summary>
        None,

		/// <summary>
		/// The node represents a <see cref="string"/> (internally referenced by index).
		/// </summary>
		StringIndex = 0xA0,

		/// <summary>
		/// The node represents a list of <see cref="ByamlPathPoint"/> instances (internally referenced by index).
		/// </summary>
		PathIndex = 0xA1,

		/// <summary>
		/// The node represents an array of dynamic child instances.
		/// </summary>
		Array = 0xC0,

		/// <summary>
		/// The node represents a dictionary of dynamic child instances referenced by a <see cref="string"/> key.
		/// </summary>
		Dictionary = 0xC1,

		/// <summary>
		/// The node represents an array of <see cref="string"/> instances.
		/// </summary>
		StringArray = 0xC2,

		/// <summary>
		/// The node represents an array of lists of <see cref="ByamlPathPoint"/> instances.
		/// </summary>
		PathArray = 0xC3,

		/// <summary>
		/// The node represents a <see cref="bool"/>.
		/// </summary>
		Boolean = 0xD0,

		/// <summary>
		/// The node represents an <see cref="int"/>.
		/// </summary>
		Integer = 0xD1,

		/// <summary>
		/// The node represents a <see cref="float"/>.
		/// </summary>
		Float = 0xD2,

		/// <summary>
		/// The node represents a <see cref="UInt32"/>.
		/// </summary>
		Uinteger = 0xD3,

		/// <summary>
		/// The node represents a <see cref="Int64"/>.
		/// </summary>
		Long = 0xD4,

		/// <summary>
		/// The node represents a <see cref="UInt64"/>.
		/// </summary>
		ULong = 0xD5,

		/// <summary>
		/// The node represents a <see cref="double"/>.
		/// </summary>
		Double = 0xD6,

		/// <summary>
		/// The node represents <c>null</c>.
		/// </summary>
		Null = 0xFF
	}

	/// <summary>
	/// Represents extension methods for <see cref="ByamlNodeType"/> instances.
	/// </summary>
	internal static class ByamlNodeTypeExtensions
	{
		/// <summary>
		/// Gets the corresponding, instantiatable <see cref="Type"/> for the given <paramref name="nodeType"/>.
		/// </summary>
		/// <param name="nodeType">The <see cref="ByamlNodeType"/> which should be instantiated.</param>
		/// <returns>The <see cref="Type"/> to instantiate for the node.</returns>
		internal static Type GetInstanceType(this ByamlNodeType nodeType)
		{
            return nodeType switch
            {
                ByamlNodeType.StringIndex => typeof(string),
                ByamlNodeType.PathIndex => typeof(List<ByamlPathPoint>),
                ByamlNodeType.Array => throw new ByamlException("Cannot instantiate an array of unknown element type."),// TODO: Check if this could be loaded as an object array.
                ByamlNodeType.Dictionary => throw new ByamlException("Cannot instantiate an object of unknown type."),// TODO: Check if this could be loaded as a string-object dictionary.
                ByamlNodeType.Boolean => typeof(bool),
                ByamlNodeType.Integer => typeof(int),
                ByamlNodeType.Float => typeof(float),
                ByamlNodeType.Uinteger => typeof(UInt32),
                ByamlNodeType.Long => typeof(Int64),
                ByamlNodeType.ULong => typeof(UInt64),
                ByamlNodeType.Double => typeof(double),
                ByamlNodeType.Null => typeof(object),
                _ => throw new ByamlException($"Unknown node type {nodeType}."),
            };
        }
	}	
}
