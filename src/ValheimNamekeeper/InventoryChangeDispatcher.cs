using System;
using System.Collections.Generic;
using System.Reflection;

namespace SuperNova.ValheimNamekeeper
{
    internal static class InventoryChangeDispatcher
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, MethodInfo> Methods = new Dictionary<Type, MethodInfo>();

        internal static void Notify(object inventory)
        {
            if (inventory == null) return;
            Type type = inventory.GetType();
            if (!Methods.TryGetValue(type, out MethodInfo method))
            {
                method = type.GetMethod("Changed", Flags, null, new[] { typeof(bool), typeof(bool) }, null)
                    ?? type.GetMethod("Changed", Flags, null, Type.EmptyTypes, null)
                    ?? type.GetMethod("OnChanged", Flags, null, Type.EmptyTypes, null);
                Methods.Add(type, method);
            }
            if (method != null)
            { method.Invoke(inventory, method.GetParameters().Length == 0 ? null : new object[] { false, false }); return; }
            if (type.GetField("m_onChanged", Flags)?.GetValue(inventory) is Delegate listeners)
            { listeners.DynamicInvoke(); return; }
            throw new MissingMethodException(type.FullName, "Changed(bool,bool)");
        }
    }
}
