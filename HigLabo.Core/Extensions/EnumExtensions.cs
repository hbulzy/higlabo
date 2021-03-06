﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace HigLabo.Core
{
    public static class EnumExtensions
    {
        private static ConcurrentDictionary<Type, MulticastDelegate> _ToStringFromEnumMethods = new ConcurrentDictionary<Type, MulticastDelegate>();

        public static String ToStringOrNullFromEnum<T>(this Nullable<T> value)
            where T : struct
        {
            if (value.HasValue == true) return ToStringFromEnum(value.Value);
            return null;
        }
        public static String ToStringFromEnum<T>(this Nullable<T> value)
            where T : struct
        {
            if (value.HasValue == true) return ToStringFromEnum(value.Value);
            return "";
        }
        public static String ToStringFromEnum<T>(this T value)
            where T : struct
        {
            var tp = typeof(T);
            if (tp.IsEnum == false) throw new ArgumentException("value must be a enum type");

            MulticastDelegate md = null;
            if (_ToStringFromEnumMethods.TryGetValue(tp, out md) == false)
            {
                var aa = tp.GetCustomAttributes(typeof(FlagsAttribute), false);
                if (aa.Length == 0)
                {
                    md = CreateToStringFromEnumFunc<T>();
                }
                _ToStringFromEnumMethods[tp] = md;
            }
            // Flags
            if (md == null) return value.ToString().Replace(" ", "");

            var f = (Func<T, String>)md;
            return f(value);
        }
        private static Func<T, String> CreateToStringFromEnumFunc<T>()
        {
            var tp = typeof(T);
            DynamicMethod dm = new DynamicMethod("ToStringFromEnum", typeof(String), new[] { tp });
            ILGenerator il = dm.GetILGenerator();
            Label defaultCase = il.DefineLabel();
            Label endOfMethod = il.DefineLabel();

            var names = Enum.GetNames(tp);
            var caseLabels = new Label[names.Length + 1];
            for (int i = 0; i < names.Length; i++)
            {
                caseLabels[i] = il.DefineLabel();
            }
            caseLabels[names.Length] = defaultCase;

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Switch, caseLabels);

            for (int i = 0; i < names.Length; i++)
            {
                // Case ??: return "";
                il.MarkLabel(caseLabels[i]);
                il.Emit(OpCodes.Ldstr, names[i]);
                il.Emit(OpCodes.Ret);
            }
            il.MarkLabel(defaultCase);
            il.ThrowException(typeof(InvalidOperationException));

            il.MarkLabel(endOfMethod);
            il.Emit(OpCodes.Ret);

            var f = typeof(Func<,>);
            var gf = f.MakeGenericType(tp, typeof(String));
            return (Func<T, String>)dm.CreateDelegate(gf);
        }
    }
}
