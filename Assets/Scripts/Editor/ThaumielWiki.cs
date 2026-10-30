using UnityEditor;
using UnityEngine;

public static class ThaumielWiki
{
    [MenuItem("Thaumiel/Wiki")]
    public static void OpenWiki()
    {
        Application.OpenURL("https://thaumiel.gitbook.io/tme-docs/documentation/tutorials/unity");
    }
}