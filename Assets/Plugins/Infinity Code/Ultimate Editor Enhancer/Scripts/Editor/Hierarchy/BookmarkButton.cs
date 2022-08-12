/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.Integration;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.HierarchyTools
{
    [InitializeOnLoad]
    public static class BookmarkButton
    {
        private static GUIStyle onStyle;
        private static GUIStyle offStyle;

        static BookmarkButton()
        {
            HierarchyItemDrawer.Register("BookmarkButton", OnHierarchyItem, -1);
        }

        private static void OnHierarchyItem(HierarchyItem item)
        {
            if (!Prefs.hierarchyBookmarks) return;
            if (Prefs.hierarchyIconsDisplayRule == HierarchyIconsDisplayRule.always) return;
            if (item.gameObject == null) return;

            Event e = Event.current;
            if (e.modifiers != EventModifiers.None) return;

            bool contain = Bookmarks.Contain(item.gameObject);
            if (!item.hovered && !contain) return;

            Rect rect = item.rect;
            Rect r = new Rect(rect.xMax - 16, rect.y, 16, rect.height);

            if (Cinemachine.ContainBrain(item.gameObject)) r.x -= 16;

            if (offStyle == null)
            {
                onStyle = new GUIStyle
                {
                    normal =
                    {
                        background = (Texture2D)Icons.starYellow
                    }
                };

                offStyle = new GUIStyle
                {
                    normal =
                    {
                        background = Styles.isProSkin? (Texture2D)Icons.starWhite: (Texture2D)Icons.starBlack
                    }
                };
            }

            if (e.type == EventType.MouseUp && e.button == 1 && r.Contains(e.mousePosition))
            {
                Bookmarks.ShowWindow();
                e.Use();
            }

            EditorGUI.BeginChangeCheck();
            bool v = EditorGUI.Toggle(r, GUIContent.none, contain, contain? onStyle: offStyle);
            if (EditorGUI.EndChangeCheck())
            {
                if (v) Bookmarks.Add(item.gameObject);
                else Bookmarks.Remove(item.gameObject);
            }
        }
    }
}