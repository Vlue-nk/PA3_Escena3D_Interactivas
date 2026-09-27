using System;
using UnityEditor;
namespace PA3.EditorTools
{
    public static class PA3Workspace
    {
        public const string Root = "Assets/_Project";
        public static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Salir de Play antes de editar recursos.");
        }
    }
}
