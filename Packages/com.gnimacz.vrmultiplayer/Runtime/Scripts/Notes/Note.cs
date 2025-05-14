using System.ComponentModel;
using UnityEngine;

[AddComponentMenu("Notes/Note"), Icon("Packages/com.gnimacz.vrmultiplayer/Editor/Assets/NoteIcon.png")]
public class Note : MonoBehaviour
{
    public bool noteFolded = false;
    public Color noteColor = new(1, 0.8f, 0.4f);
    public Color noteTextColor = Color.black;
    public string noteBody = "";
}