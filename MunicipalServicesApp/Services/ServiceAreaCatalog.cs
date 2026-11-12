using System;
using System.Collections.Generic;
using MunicipalServicesApp.DataStructures;

namespace MunicipalServicesApp.Services
{
    /// <summary>
    /// The municipality's service areas and the main roads between neighbouring areas,
    /// plus the matching of a request's free-text location to an area.
    ///
    /// The distances are approximate, rounded demo values (kilometres), not surveyed data.
    /// Requests whose location matches no area are "Unassigned" and stay outside the graph.
    /// </summary>
    public static class ServiceAreaCatalog
    {
        /// <summary>Same text as the default Issue.Area.</summary>
        public const string Unassigned = "Unassigned";

        private static readonly string[] AreaNames =
        {
            "Soweto", "Braamfontein", "Alexandra", "Sandton", "Randburg",
            "Fourways", "Diepsloot", "Midrand", "Roodepoort", "Orange Farm"
        };

        private class Road
        {
            public readonly string From;
            public readonly string To;
            public readonly int Km;

            public Road(string from, string to, int km)
            {
                From = from;
                To = to;
                Km = km;
            }
        }

        // All distances are different, so the minimum spanning tree is unique
        private static readonly Road[] Roads =
        {
            new Road("Alexandra", "Sandton", 4),
            new Road("Sandton", "Braamfontein", 11),
            new Road("Alexandra", "Braamfontein", 13),
            new Road("Braamfontein", "Soweto", 12),
            new Road("Braamfontein", "Randburg", 14),
            new Road("Sandton", "Randburg", 9),
            new Road("Randburg", "Fourways", 10),
            new Road("Sandton", "Fourways", 15),
            new Road("Fourways", "Diepsloot", 8),
            new Road("Randburg", "Diepsloot", 17),
            new Road("Fourways", "Midrand", 16),
            new Road("Sandton", "Midrand", 20),
            new Road("Diepsloot", "Midrand", 19),
            new Road("Randburg", "Roodepoort", 18),
            new Road("Roodepoort", "Soweto", 21),
            new Road("Roodepoort", "Braamfontein", 22),
            new Road("Soweto", "Orange Farm", 30)
        };

        public static IReadOnlyList<string> Areas
        {
            get { return Array.AsReadOnly(AreaNames); }
        }

        public static bool IsKnownArea(string name)
        {
            if (name == null)
                return false;

            foreach (string area in AreaNames)
            {
                if (string.Equals(area, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>A new graph of the areas (vertices) and the roads between them (edges). Area names are not case-sensitive.</summary>
        public static Graph<string> BuildGraph()
        {
            var graph = new Graph<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string area in AreaNames)
                graph.AddVertex(area);

            foreach (Road road in Roads)
                graph.AddEdge(road.From, road.To, road.Km);

            return graph;
        }

        /// <summary>
        /// Finds the service area named in a free-text location. The text after the last
        /// comma is checked first (addresses usually end with the area, as in
        /// "Alexandra Road, Sandton"), then the parts before it. Returns "Unassigned" if
        /// no area is named.
        /// </summary>
        public static string FindArea(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return Unassigned;

            string[] parts = location.Split(',');
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                    continue;

                foreach (string area in AreaNames)
                {
                    if (part.IndexOf(area, StringComparison.OrdinalIgnoreCase) >= 0)
                        return area;
                }
            }

            return Unassigned;
        }
    }
}