/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.IO;
using InfinityCode.UltimateEditorEnhancer.Interceptors;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public static bool _changeNumberFieldValueByArrow = true;
        public static bool _hierarchyTypeFilter = true;
        public static bool _searchInEnumFields = true;
        public static bool _unsafeFeatures = true;
        public static int searchInEnumFieldsMinValues = 6;

        private static int hasUnsafeBlock = -1; // -1 - unknown, 0 - no block, 1 - has block

        public static bool changeNumberFieldValueByArrow
        {
            get { return _changeNumberFieldValueByArrow && unsafeFeatures; }
        }

        public static bool hierarchyTypeFilter
        {
            get { return _hierarchyTypeFilter && unsafeFeatures; }
        }

        public static bool searchInEnumFields
        {
            get { return _searchInEnumFields && unsafeFeatures; }
        }

        public static bool unsafeFeatures
        {
            get
            {
                if (hasUnsafeBlock == -1)
                {
                    hasUnsafeBlock = File.Exists("UEENoUnsafe.txt") ? 1 : 0;
                }
                return _unsafeFeatures && hasUnsafeBlock == 0;
            }
        }

        public class UnsafeManager: StandalonePrefManager<UnsafeManager>
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return new[]
                    {
                        "Unsafe",
                    };
                }
            }

            private void DrawToggleField(string label, ref bool value, Action OnChange)
            {
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.ToggleLeft(label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    if (OnChange != null) OnChange();
                }
            }

            public override void Draw()
            {
                EditorGUI.BeginChangeCheck();

                _unsafeFeatures = EditorGUILayout.ToggleLeft("Unsafe Features", _unsafeFeatures);

                if (EditorGUI.EndChangeCheck())
                {
                    EnumPopupInterceptor.Refresh();
                    HierarchyToolbarInterceptor.Refresh();
                    NumberFieldInterceptor.Refresh();
                }

                EditorGUI.BeginDisabledGroup(!_unsafeFeatures);

                DrawToggleField("Change Number Fields Value By Arrows", ref _changeNumberFieldValueByArrow, NumberFieldInterceptor.Refresh);
                DrawToggleField("Hierarchy Type Filter", ref _hierarchyTypeFilter, HierarchyToolbarInterceptor.Refresh);
                DrawToggleField("Search In Enum Fields", ref _searchInEnumFields, EnumPopupInterceptor.Refresh);

                if (_searchInEnumFields)
                {
                    EditorGUI.indentLevel++;
                    searchInEnumFieldsMinValues = EditorGUILayout.IntField("Min Values", searchInEnumFieldsMinValues);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndDisabledGroup();
            }
        }
    }
}