using System;
using System.Collections.Generic;

namespace SuperNova.ValheimNamekeeper
{
    // Nexus values are read, never changed or removed. Reset markers belong to Namekeeper.
    internal static class PersonalizedText
    {
        internal const string NameKey = "com.supernova.namekeeper.name";
        internal const string DescriptionKey = "com.supernova.namekeeper.description";
        internal const string ResetNameKey = "com.supernova.namekeeper.original-name";
        internal const string ResetDescriptionKey = "com.supernova.namekeeper.original-description";
        internal const string NexusNameKey = "com.supernova.valheimnexus.itemname.v1";
        internal const string NexusDescriptionKey = "com.supernova.valheimnexus.itemdescription.v1";

        internal static string Read(IDictionary<string, string> data, string key) =>
            data != null && data.TryGetValue(key, out string value) ? value : null;

        internal static bool Owns(IDictionary<string, string> data, bool description) =>
            description ? Read(data, DescriptionKey) != null || Read(data, ResetDescriptionKey) == "1"
                : !string.IsNullOrEmpty(Read(data, NameKey)) || Read(data, ResetNameKey) == "1";

        internal static string Resolve(IDictionary<string, string> data, string original, bool description)
        {
            string custom = Read(data, description ? DescriptionKey : NameKey);
            if (description ? custom != null : !string.IsNullOrEmpty(custom)) return custom;
            if (Read(data, description ? ResetDescriptionKey : ResetNameKey) == "1") return original;
            string nexus = Read(data, description ? NexusDescriptionKey : NexusNameKey);
            return string.IsNullOrEmpty(nexus) ? original : nexus;
        }

        internal static bool Changes(IDictionary<string, string> data, string original,
            string text, bool reset, bool description)
        {
            if (!reset) return !string.Equals(text, Resolve(data, original, description), StringComparison.Ordinal);
            return Read(data, description ? DescriptionKey : NameKey) != null ||
                !string.Equals(Resolve(data, original, description), original, StringComparison.Ordinal);
        }

        internal static void Apply(IDictionary<string, string> data, string original,
            string text, bool reset, bool description)
        {
            string key = description ? DescriptionKey : NameKey;
            string resetKey = description ? ResetDescriptionKey : ResetNameKey;
            if (reset || string.Equals(text, original, StringComparison.Ordinal))
            { data.Remove(key); data[resetKey] = "1"; }
            else
            { data[key] = text; data.Remove(resetKey); }
        }

        internal static string ReplaceDescription(string body, string replacement, params string[] originals)
        {
            // Match only the known leading description, never a stat or another mod's paragraph.
            string match = null;
            foreach (string candidate in originals)
                if (!string.IsNullOrEmpty(candidate) && body.StartsWith(candidate, StringComparison.Ordinal) &&
                    (match == null || candidate.Length > match.Length)) match = candidate;
            if (match != null)
            {
                string remainder = body.Substring(match.Length);
                // Nexus prepends one separator when the original description is empty.
                if (string.IsNullOrEmpty(originals[0]) && replacement.Length == 0 && remainder.StartsWith("\n", StringComparison.Ordinal))
                    remainder = remainder.Substring(1);
                return replacement + remainder;
            }
            bool emptyOriginal = originals.Length > 0 && string.IsNullOrEmpty(originals[0]);
            return emptyOriginal && replacement.Length > 0 ? replacement + "\n\n" + body : body;
        }

        internal static string ReplaceHoverName(string body, string replacement, params string[] names)
        {
            int lineBreak = body.IndexOf('\n');
            string firstLine = lineBreak < 0 ? body : body.Substring(0, lineBreak);
            string match = null;
            foreach (string name in names)
                if (!string.IsNullOrEmpty(name) && firstLine.StartsWith(name, StringComparison.Ordinal) &&
                    (match == null || name.Length > match.Length)) match = name;
            string suffix = match == null ? "" : firstLine.Substring(match.Length);
            return replacement + suffix + (lineBreak < 0 ? "" : body.Substring(lineBreak));
        }
    }
}
