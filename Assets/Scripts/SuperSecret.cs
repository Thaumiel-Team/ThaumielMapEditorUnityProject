using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
    [InitializeOnLoad]
    public static class SuperSecret
    {
        static SuperSecret()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (Random.Range(0, 100000) == 1)
            {
                Debug.Log(":3");
            }
        }
    }
}
