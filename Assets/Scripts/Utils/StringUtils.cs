using System;
using System.Data;

namespace Utils {
    using System.Text;
    using System.Collections.Generic;
    using JetBrains.Annotations;
    using Object = UnityEngine.Object;

    public static class StringUtils {
        public static string ToString([CanBeNull] List<Object> list, string delimiter = "\n") {
            if (list == null) {
                return "null";
            }

            int lastIndex = list.Count - 1;
            if (lastIndex == -1) {
                return "{}";
            }

            var builder = new StringBuilder(500);
            builder.Append('{');
            for (int n = 0; n < lastIndex; n++) {
                Append(list[n], builder);
                builder.Append(delimiter);
            }

            Append(list[lastIndex], builder);
            builder.Append('}');

            return builder.ToString();
        }

        public static string ToString([CanBeNull] Dictionary<string, string> dict, string delimiter = "\n") {
            if (dict == null) {
                return "null";
            }

            var builder = new StringBuilder(500);
            foreach (KeyValuePair<string, string> d in dict) {
                builder.Append(d.Key);
                builder.Append(": ");
                builder.Append(d.Value);
                builder.Append(delimiter);
            }

            return builder.ToString();
        }

        public static string ToString(float[] list, string delimiter = ", ") {
            if (list == null) {
                return "null";
            }

            int lastIndex = list.Length - 1;
            if (lastIndex == -1) {
                return "{}";
            }

            var builder = new StringBuilder(500);
            builder.Append('{');
            for (int n = 0; n < lastIndex; n++) {
                builder.Append(list[n].ToString());
                builder.Append(delimiter);
            }

            builder.Append(list[lastIndex].ToString());
            builder.Append('}');

            return builder.ToString();
        }

        public static string ToString(int[] list, string delimiter = ", ") {
            if (list == null) {
                return "null";
            }

            int lastIndex = list.Length - 1;
            if (lastIndex == -1) {
                return "{}";
            }

            var builder = new StringBuilder(500);
            builder.Append('{');
            for (int n = 0; n < lastIndex; n++) {
                builder.Append(list[n].ToString());
                builder.Append(delimiter);
            }

            builder.Append(list[lastIndex].ToString());
            builder.Append('}');

            return builder.ToString();
        }

        public static void Append(Object target, StringBuilder toBuilder) {
            if (target == null) {
                toBuilder.Append("null");
            }
            else {
                toBuilder.Append("\"");
                toBuilder.Append(target.name);
                toBuilder.Append("\" (");
                toBuilder.Append(target.GetType().Name);
                toBuilder.Append(")");
            }
        }
    }
}