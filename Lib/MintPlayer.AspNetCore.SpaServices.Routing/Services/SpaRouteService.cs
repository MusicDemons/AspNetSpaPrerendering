﻿using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace MintPlayer.AspNetCore.SpaServices.Routing
{
    public interface ISpaRouteService
    {
        /// <summary>Returns the SPA route (if any) that matches the requested URL.</summary>
        /// <param name="httpContext">The current HTTP context</param>
        SpaRoute GetCurrentRoute(HttpContext httpContext);

        /// <summary>Generates an url for a SPA route.</summary>
        /// <param name="routeName">Name of the SPA route</param>
        /// <param name="parameters">Dictionary containing a key-value mapping for the parameters</param>
        string GenerateUrl(string routeName, Dictionary<string, object> parameters);

        /// <summary>Generates an url for a SPA route.</summary>
        /// <typeparam name="T">Some anonymous type.</typeparam>
        /// <param name="routeName">Name of the SPA route as defined in the AddSpaRoutes call.</param>
        /// <param name="parameters">Anonymous object containing the key-value mapping for the parameters of the SPA route.</param>
        string GenerateUrl<T>(string routeName, T parameters);
    }

    internal class SpaRouteService : ISpaRouteService
    {
        public SpaRouteService(SpaRouteBuilder routeBuilder)
        {
            this.routeBuilder = routeBuilder;
        }

        private const string rgx_keys = @"(?<=\{)[a-zA-Z0-9]+(?=\})";
        private SpaRouteBuilder routeBuilder;

        /// <summary>Build result</summary>
        private IEnumerable<Data.ISpaRouteItem> spaRouteItems;

        /// <summary>Ensures that the routeBuilder delegate has been executed.</summary>
        private void EnsureSpaRoutesBuilt()
        {
            if (spaRouteItems == null)
                spaRouteItems = routeBuilder.Build();
        }

        /// <summary>Generates an url for a SPA route.</summary>
        /// <param name="routeName">Name of the SPA route</param>
        /// <param name="parameters">Dictionary containing a k
        public string GenerateUrl(string routeName, Dictionary<string, object> parameters)
        {
            return GenerateUrlBase(routeName, parameters);
        }

        /// <summary>Generates an url for a SPA route.</summary>
        /// <typeparam name="T">Some anonymous type.</typeparam>
        /// <param name="routeName">Name of the SPA route as defined in the AddSpaRoutes call.</param>
        /// <param name="parameters">Anonymous object containing the key-value mapping for the parameters of the SPA route.</param>
        public string GenerateUrl<T>(string routeName, T parameters)
        {
            var values = typeof(T).GetProperties().ToDictionary(p => p.Name, p => p.GetValue(parameters));
            return GenerateUrlBase(routeName, values);
        }

        private string GenerateUrlBase(string routeName, IDictionary<string, object> parameters)
        {
            EnsureSpaRoutesBuilt();

            var route = spaRouteItems.FirstOrDefault(r => r.FullName == routeName);
            if (route == null) throw new System.Exception($"Route with name {routeName} not found.");

            var rgx_keys = new Regex(@"\{(?<key>[a-zA-Z0-9]+)\}");
            var urlWithoutQuery = rgx_keys.Replace($"/{route.FullPath}", m => parameters[m.Groups["key"].Value].ToString());
            var present_param_keys = rgx_keys.Matches(route.FullPath).Select(m => m.Groups["key"].Value);
            var excessive_param_keys = parameters.Keys.Except(present_param_keys);
            var query = string.Join('&', excessive_param_keys.Select((key) => $"{key}={parameters[key]}"));

            if (excessive_param_keys.Any())
            {
                return $"{urlWithoutQuery}?{query}";
            }
            else
            {
                return urlWithoutQuery;
            }
        }

        /// <summary>Returns the SPA route (if any) that matches the requested URL.</summary>
        /// <param name="httpContext">The current HTTP context</param>
        public SpaRoute GetCurrentRoute(HttpContext httpContext)
        {
            EnsureSpaRoutesBuilt();

            // Find the SPA route for the current request
            var match = spaRouteItems.FirstOrDefault(r => IsMatch(GetCurrentPath(httpContext), r.FullPath));

            if (match == null)
            {
                return null;
            }
            else if (!string.IsNullOrEmpty(match.FullPath))
            {
                // Get current path
                string url, query;
                GetCurrentPath(httpContext, out url, out query);

                // Get parameter names
                var parameter_keys = Regex.Matches(match.FullPath, rgx_keys).Select(m => m.Value).ToList(); // [id, ...]

                var rgx_values = PlaceholderString2WildcardString(match.FullPath);
                var parameter_match = Regex.Match(url, rgx_values);
                if (!parameter_match.Success) throw new System.Exception("Unexpected exception: parameter match should be successful");

                var parameter_groups = new Group[parameter_match.Groups.Count];
                parameter_match.Groups.CopyTo(parameter_groups, 0);

                var parameter_values = parameter_groups.Where(g => g.GetType() == typeof(Group)).Select(g => g.Value).ToList();
                if (parameter_keys.Count != parameter_values.Count) throw new System.Exception("Unexpected exception: number of keys and values should be equal");

                return new SpaRoute
                {
                    Name = match.FullName,
                    Path = match.FullPath,
                    Parameters = Enumerable.Range(0, parameter_keys.Count).ToDictionary(
                        (index) => parameter_keys[index],
                        (index) => parameter_values[index]
                    ),
                    QueryParameters = query.Split('&').Select(t =>
                    {
                        var split = t.Split('=', 2);
                        return new
                        {
                            Key = split[0],
                            Value = split.Length > 1 ? split[1] : null
                        };
                    }).ToDictionary(t => t.Key, t => t.Value)
                };
            }
            else
            {
                return new SpaRoute
                {
                    Name = match.FullName,
                    Path = match.FullPath,
                    Parameters = new Dictionary<string, string>(),
                    QueryParameters = new Dictionary<string, string>()
                };
            }
        }

        /// <summary>Tests if an url [/manage/person/3/edit] matches a placeholder-url [/manage/person/{person_id}/edit].</summary>
        /// <param name="path">The visited URL</param>
        /// <param name="route">URL of the route containing placeholders [/manage/person/{person_id}/edit]</param>
        private bool IsMatch(string path, string route)
        {
            var formatted_route = PlaceholderString2WildcardString(route);
            return Regex.IsMatch(path, $"^/{formatted_route}$");
        }

        /// <summary>Converts an url with placeholders [/manage/person/{person_id}/edit] to a string ready to be used as Regex [/manage/person/(.+)/edit].</summary>
        /// <param name="input">Placeholder string</param>
        private string PlaceholderString2WildcardString(string input)
        {
            var rgx = @"\{[a-zA-Z0-9]+\}";
            var replace = @"([^\/]+)";
            var wildcardString = Regex.Replace(input, rgx, replace);
            return wildcardString;
        }

        /// <summary>Retrieves the url visited by the user.</summary>
        /// <param name="context">Http Context</param>
        private string GetCurrentPath(HttpContext context)
        {
            string url, query;
            GetCurrentPath(context, out url, out query);
            return url;
        }

        /// <summary>Retrieves the url visited by the user.</summary>
        /// <param name="context">Http Context</param>
        private void GetCurrentPath(HttpContext context, out string url, out string query)
        {
            // For an angular app this instruction returns
            // - The correct path in Development mode
            // - index.html in Production mode
            //return context.Request.Path;

            // The RawTarget private property contains the real path visited by the user at any time.
            var fc = context.Features.GetType();
            var rt = fc.GetProperty("RawTarget");
            var path = (string)rt.GetValue(context.Features);

            var queryStart = path.LastIndexOf('?');

            url = path.Substring(0, queryStart);
            query = path.Substring(queryStart + 1);
        }
    }
}