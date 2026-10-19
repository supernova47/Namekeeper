using System;
using System.Collections.Generic;
using System.Reflection;

namespace SuperNova.ValheimNamekeeper
{
    internal static class TooltipTitleUpdater
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, Accessor> Cache = new Dictionary<Type, Accessor>();

        internal static void SetTitle(object tooltip, string title)
        {
            if (tooltip == null) return;
            Type type = tooltip.GetType();
            if (!Cache.TryGetValue(type, out Accessor accessor))
            { accessor = new Accessor(type); Cache.Add(type, accessor); }
            // Set refreshes a visible tooltip; writing m_topic alone does not.
            accessor.Set.Invoke(tooltip, new[] { title, accessor.Text.GetValue(tooltip),
                accessor.Anchor.GetValue(tooltip), accessor.Position.GetValue(tooltip) });
        }

        private sealed class Accessor
        {
            internal readonly FieldInfo Text, Anchor, Position;
            internal readonly MethodInfo Set;
            internal Accessor(Type type)
            {
                Text = FindField(type, "m_text");
                Anchor = FindField(type, "m_anchor");
                Position = FindField(type, "m_fixedPosition");
                Set = type.GetMethod("Set", Flags, null,
                    new[] { typeof(string), typeof(string), Anchor.FieldType, Position.FieldType }, null)
                    ?? throw new MissingMethodException(type.FullName, "Set(string,string,anchor,position)");
            }
            private static FieldInfo FindField(Type type, string name)
            {
                for (Type current = type; current != null; current = current.BaseType)
                {
                    FieldInfo field = current.GetField(name, Flags | BindingFlags.DeclaredOnly);
                    if (field != null) return field;
                }
                throw new MissingFieldException(type.FullName, name);
            }
        }
    }
}
