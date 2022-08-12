/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using InfinityCode.UltimateEditorEnhancer.Interceptors;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.InspectorTools
{
    [InitializeOnLoad]
    public static class AnimatorInspectorClips
    {
        private static bool showClips = false;
        private static bool inited;
        private static GUIContent playContent;
        private static GUIContent stopContent;
        private static GUIContent selectContent;
        private static int playIndex = -1;
        private static Animator animator;
        private static AnimationClip clip;
        private static double startTime;

        static AnimatorInspectorClips()
        {
            AnimatorInspectorInterceptor.OnInspectorGUI += OnInspectorGUI;
        }

        private static void EditorUpdate()
        {
            clip.SampleAnimation(animator.gameObject, (float)(EditorApplication.timeSinceStartup - startTime) % clip.length);
        }

        private static void Init()
        {
            inited = true;
            playContent = new GUIContent(EditorIconContents.playButtonOn.image, "Play");
            stopContent = new GUIContent(Resources.LoadIcon("Stop"), "Stop");
            selectContent = new GUIContent(EditorIconContents.rectTransformBlueprint.image, "Select");
        }

        private static void OnInspectorGUI(Editor editor)
        {
            if (!Prefs.animatorInspectorClips) return;
            if (EditorApplication.isPlaying) return;
            if (editor.targets.Length != 1) return;

            Animator animator = editor.target as Animator;
            if (animator.runtimeAnimatorController == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            showClips = EditorGUILayout.Foldout(showClips, "Clips");
            EditorGUI.indentLevel--;
            if (!showClips)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            if (!inited) Init();

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null) continue;
                EditorGUILayout.BeginHorizontal();

                bool isPlayed = playIndex == i;
                if (isPlayed)
                {
                    if (GUILayout.Button(stopContent, GUILayout.Width(30), GUILayout.Height(20)))
                    {
                        Stop();
                    }
                }
                else
                {
                    if (GUILayout.Button(playContent, GUILayout.Width(30), GUILayout.Height(20)))
                    {
                        Play(animator, clip, i);
                    }
                }

                if (GUILayout.Button(selectContent, GUILayout.Width(30), GUILayout.Height(20)))
                {
                    Selection.activeObject = clip;
                }

                EditorGUILayout.LabelField(clip.name, EditorStyles.textField);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            Stop();
        }

        private static void Play(Animator _animator, AnimationClip _clip, int index)
        {
            playIndex = index;
            animator = _animator;
            clip = _clip;

            startTime = EditorApplication.timeSinceStartup;

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;

            Selection.selectionChanged -= Stop;
            Selection.selectionChanged += Stop;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void Stop()
        {
            if (clip == null) return;
            if (animator == null) return;

            clip.SampleAnimation(animator.gameObject, 0);

            playIndex = -1;
            clip = null;
            animator = null;

            Selection.selectionChanged -= Stop;
            EditorApplication.update -= EditorUpdate;
        }
    }
}