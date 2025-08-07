#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCqrsEventSourcing.Presentation.Controllers;

/// <summary>
/// System.Text.Json serialization extensions for <see cref="AccountsController"/>.
/// Provides strongly-typed JSON serialization/deserialization methods.
/// </summary>
public static sealed class AccountsControllerJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles
	};

	/// <summary>
	/// Serializes the <see cref="AccountsController"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The controller instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the controller.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this AccountsController value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		return JsonSerializer.Serialize(value, indented ? _jsonSerializerOptionsWithIndentation : _jsonSerializerOptions);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="AccountsController"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized controller instance, or null if JSON is empty or whitespace.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
	public static AccountsController? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<AccountsController>(json, _jsonSerializerOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="AccountsController"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">The deserialized controller instance, or null if deserialization fails.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
	public static bool TryFromJson(string json, out AccountsController? value)
	{
		value = null;

		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}

		try
		{
			value = JsonSerializer.Deserialize<AccountsController>(json, _jsonSerializerOptions);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	private static readonly JsonSerializerOptions _jsonSerializerOptionsWithIndentation = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles
	};
}