using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Note))]
[Icon("Packages/com.gnimacz.vrmultiplayer/Editor/Assets/NoteIcon.png")]
public class NoteEditor : Editor
{
    private static GUIStyle _noteStyle = null;

    private static GUILayoutOption[] TEXT_AREA_OPTIONS =
    {
        GUILayout.ExpandWidth(true),
        GUILayout.ExpandHeight(false)
    };

    static void generateBackgroundTexturesForDescription(Note note)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, note.noteColor);
        texture.Apply();

        GUIStyle style = new GUIStyle();
        style.normal.background = texture;
        style.wordWrap = true;
        style.fontSize = 12;
        style.padding = new RectOffset(5, 5, 5, 5);
        style.margin = new RectOffset(5, 5, 5, 5);
        _noteStyle = style;
    }

    private Note _note;

    private void OnEnable()
    {
        _note = target as Note;
        generateBackgroundTexturesForDescription(_note);
    }

    public override void OnInspectorGUI()
    {
        _note.noteFolded = EditorGUILayout.Foldout(_note.noteFolded, "Note Color", true);
        if (_note.noteFolded)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Note Body Color");
            Color newNoteColor = EditorGUILayout.ColorField(_note.noteColor);
            if (newNoteColor != _note.noteColor)
            {
                _note.noteColor = newNoteColor;
                generateBackgroundTexturesForDescription(_note);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Note Text Color");
            Color newNoteTextColor = EditorGUILayout.ColorField(_note.noteTextColor);
            if (newNoteTextColor != _note.noteTextColor)
            {
                _note.noteTextColor = newNoteTextColor;
                _noteStyle.normal.textColor = newNoteTextColor;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        _note.noteBody = EditorGUILayout.TextArea(_note.noteBody, _noteStyle, TEXT_AREA_OPTIONS);
    }
}