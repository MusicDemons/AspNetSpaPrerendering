// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace MintPlayer.AspNetCore.SpaServices.Prerendering;

/// <summary>
/// Describes the prerendering result returned by JavaScript code.
/// </summary>
[Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
public class RenderToStringResult
{
	/// <summary>
	/// If set, specifies JSON-serializable data that should be added as a set of global JavaScript variables in the document.
	/// This can be used to transfer arbitrary data from server-side prerendering code to client-side code (for example, to
	/// transfer the state of a Redux store).
	/// </summary>
	public JObject Globals { get; set; }

	/// <summary>
	/// The HTML generated by the prerendering logic.
	/// </summary>
	public string Html { get; set; }

	/// <summary>
	/// If set, specifies that instead of rendering HTML, the response should be an HTTP redirection to this URL.
	/// This can be used if the prerendering code determines that the requested URL would lead to a redirection according
	/// to the SPA's routing configuration.
	/// </summary>
	public string RedirectUrl { get; set; }

	/// <summary>
	/// If set, specifies the HTTP status code that should be sent back with the server response.
	/// </summary>
	public int? StatusCode { get; set; }

	/// <summary>
	/// Constructs a block of JavaScript code that assigns data from the
	/// <see cref="Globals"/> property to the global namespace.
	/// </summary>
	/// <returns>A block of JavaScript code.</returns>
	public string CreateGlobalsAssignmentScript()
	{
		if (Globals == null)
		{
			return string.Empty;
		}

		var stringBuilder = new StringBuilder();

		foreach (var property in Globals.Properties())
		{
			var propertyNameJavaScriptString = JavaScriptEncoder.Default.Encode(property.Name);
			var valueJson = property.Value.ToString(Formatting.None);
			var valueJsonJavaScriptString = JavaScriptEncoder.Default.Encode(valueJson);

			stringBuilder.AppendFormat("window[\"{0}\"] = JSON.parse(\"{1}\");",
				propertyNameJavaScriptString,
				valueJsonJavaScriptString);
		}

		return stringBuilder.ToString();
	}
}
