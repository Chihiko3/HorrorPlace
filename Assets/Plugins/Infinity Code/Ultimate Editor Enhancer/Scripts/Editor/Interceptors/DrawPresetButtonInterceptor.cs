/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class DrawPresetButtonInterceptor: StatedInterceptor<DrawPresetButtonInterceptor>
    {
        protected override InitType initType
        {
            get { return InitType.gui; }
        }

        protected override MethodInfo originalMethod
        {
            get { return PresetSelectorRef.drawPresetButtonMethod; }
        }

        protected override string prefixMethodName
        {
            get { return nameof(DrawPresetButtonPrefix); }
        }

        public override bool state
        {
            get { return Prefs.hidePresetButton; }
        }

        private static bool DrawPresetButtonPrefix()
        {
            return !Prefs.hidePresetButton;
        }
    }
}