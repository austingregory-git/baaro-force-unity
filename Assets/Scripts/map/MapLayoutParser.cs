using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Map
{
    /// <summary>
    /// Parses the hand-authored .map text format into a <see cref="MapLayout"/>.
    ///
    /// File shape:
    /// <code>
    /// # lines starting with # are comments; blank lines are ignored
    /// WIDTH=10
    /// HEIGHT=10
    ///
    /// [TERRAIN]
    /// GR GR GR GR GR GR GR GR GR GR
    /// GR GR FO FO GR GR GR GR GR GR
    /// ... (HEIGHT rows, WIDTH space-separated codes each — see MapCodeTables.Terrain)
    ///
    /// [OBJECTS]
    /// .. .. .. .. .. .. .. .. .. ..
    /// ... (optional section — omit entirely for an all-empty object layer)
    ///
    /// [UNITS]
    /// PZ PZ .. .. .. .. .. .. .. ..
    /// ... (optional section — ".." empty, "PZ" player deploy tile, else an Npc code)
    /// </code>
    ///
    /// The first grid row is the FAR edge of the map (z = Height-1, the enemy side); the last
    /// grid row is the NEAR edge (z = 0, the player side) — so the file reads top-to-bottom the
    /// same way the battlefield does. Columns read left-to-right as x = 0..Width-1.
    ///
    /// WIDTH and HEIGHT must currently be equal — the rest of the map pipeline (TurnManager,
    /// DeploymentManager, camera framing) assumes one square grid dimension throughout, so
    /// non-square layouts are rejected with a clear error rather than silently misbehaving.
    /// </summary>
    public static class MapLayoutParser
    {
        /// <summary>Loads and parses a .map file from Resources (e.g. "Maps/wolf_den" for
        /// Assets/Resources/Maps/wolf_den.map). Returns null (and logs) if the asset can't be
        /// found or the text fails to parse — callers should fall back gracefully rather than
        /// let a bad map file take the whole fight down.</summary>
        public static MapLayout Load(string resourcesPath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcesPath);
            if (asset == null)
            {
                Debug.LogError($"[MapLayoutParser] Map file not found at Resources/{resourcesPath}.map");
                return null;
            }

            try
            {
                return Parse(asset.text);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[MapLayoutParser] Failed to parse '{resourcesPath}': {e.Message}");
                return null;
            }
        }

        /// <summary>Parses raw .map text. Throws FormatException with a descriptive message on
        /// any malformed input — see <see cref="Load"/> for the forgiving wrapper around this.</summary>
        public static MapLayout Parse(string text)
        {
            string[] rawLines = text.Replace("\r\n", "\n").Split('\n');

            int width  = -1;
            int height = -1;
            int lineIndex = 0;

            // ---- WIDTH=/HEIGHT= header, before the first section ----
            while (lineIndex < rawLines.Length)
            {
                string line = StripComment(rawLines[lineIndex]);
                lineIndex++;
                if (line.Length == 0) continue;
                if (line.StartsWith("[")) { lineIndex--; break; } // first section header — header block done

                string[] kv = line.Split('=');
                if (kv.Length != 2) throw new FormatException($"Expected KEY=VALUE, got '{line}'");
                string key = kv[0].Trim().ToUpperInvariant();
                if (!int.TryParse(kv[1].Trim(), out int value))
                    throw new FormatException($"'{kv[1].Trim()}' is not a number (line: '{line}')");

                if (key == "WIDTH") width = value;
                else if (key == "HEIGHT") height = value;
                else throw new FormatException($"Unknown header key '{key}' — expected WIDTH or HEIGHT");
            }

            if (width <= 0 || height <= 0)
                throw new FormatException("Missing or invalid WIDTH=/HEIGHT= header");
            if (width != height)
                throw new FormatException(
                    $"WIDTH ({width}) must equal HEIGHT ({height}) — non-square maps aren't supported yet");

            var layout = new MapLayout { Width = width, Height = height };
            bool sawTerrain = false;

            while (lineIndex < rawLines.Length)
            {
                string header = StripComment(rawLines[lineIndex]);
                lineIndex++;
                if (header.Length == 0) continue;

                switch (header.ToUpperInvariant())
                {
                    case "[TERRAIN]":
                        layout.Terrain = ParseGrid(rawLines, ref lineIndex, width, height, "TERRAIN",
                            (code, x, z) =>
                            {
                                if (!MapCodeTables.Terrain.TryGetValue(code, out var terrain))
                                    throw new FormatException($"Unknown terrain code '{code}' at ({x},{z})");
                                return terrain;
                            });
                        sawTerrain = true;
                        break;

                    case "[OBJECTS]":
                        layout.Objects = ParseGrid(rawLines, ref lineIndex, width, height, "OBJECTS",
                            (code, x, z) =>
                            {
                                if (code == MapCodeTables.EmptyCode) return TileObjectType.None;
                                if (!MapCodeTables.Objects.TryGetValue(code, out var obj))
                                    throw new FormatException($"Unknown object code '{code}' at ({x},{z})");
                                return obj;
                            });
                        break;

                    case "[UNITS]":
                        ParseUnitsGrid(rawLines, ref lineIndex, width, height, layout);
                        break;

                    default:
                        throw new FormatException($"Unknown section header '{header}'");
                }
            }

            if (!sawTerrain)
                throw new FormatException("Missing required [TERRAIN] section");

            // Sections the file omitted entirely default to "all empty" rather than null, so
            // MapGenerator never has to null-check them.
            if (layout.Objects == null) layout.Objects = new TileObjectType[width, height];

            return layout;
        }

        /// <summary>Reads exactly <paramref name="height"/> rows of <paramref name="width"/>
        /// space-separated tokens starting at <paramref name="lineIndex"/> (advancing it past
        /// them), converting each token via <paramref name="convert"/>. File row 0 is the FAR
        /// edge (z = height-1) — see the class doc comment.</summary>
        private static T[,] ParseGrid<T>(string[] rawLines, ref int lineIndex, int width, int height,
            string sectionName, Func<string, int, int, T> convert)
        {
            var grid = new T[width, height];
            int rowsRead = 0;

            while (rowsRead < height)
            {
                if (lineIndex >= rawLines.Length)
                    throw new FormatException(
                        $"[{sectionName}] section ended early — expected {height} rows, got {rowsRead}");

                string line = StripComment(rawLines[lineIndex]);
                lineIndex++;
                if (line.Length == 0) continue;

                string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != width)
                    throw new FormatException(
                        $"[{sectionName}] row {rowsRead} has {tokens.Length} cells, expected {width} ('{line}')");

                int z = height - 1 - rowsRead; // first file row = far edge, see class doc comment
                for (int x = 0; x < width; x++)
                    grid[x, z] = convert(tokens[x].ToUpperInvariant(), x, z);

                rowsRead++;
            }

            return grid;
        }

        private static void ParseUnitsGrid(string[] rawLines, ref int lineIndex, int width, int height,
            MapLayout layout)
        {
            ParseGrid<object>(rawLines, ref lineIndex, width, height, "UNITS", (code, x, z) =>
            {
                if (code == MapCodeTables.EmptyCode) return null;
                if (code == MapCodeTables.PlayerDeployCode) { layout.DeploymentTiles.Add((x, z)); return null; }
                if (!MapCodeTables.Units.TryGetValue(code, out var factory))
                    throw new FormatException($"Unknown unit code '{code}' at ({x},{z})");
                layout.EnemySpawns.Add((x, z, factory));
                return null;
            });
        }

        private static string StripComment(string rawLine)
        {
            int hash = rawLine.IndexOf('#');
            string line = hash >= 0 ? rawLine.Substring(0, hash) : rawLine;
            return line.Trim();
        }
    }
}
