using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LineGround))]
public class LineGroundExtension : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LineGround lineGround = (LineGround)target;
        if(GUILayout.Button("땅 만들기"))
        {
            lineGround.CreateGround();
        }

        if(GUILayout.Button("점 추가"))
        {
            lineGround.points.Add(lineGround.gameObject.transform.position);
        }

        if(GUILayout.Button("마지막 점 제거"))
        {
            try
            {
                lineGround.points.RemoveAt(lineGround.points.Count - 1);
            }
            catch 
            {

            }
        }    
    }
}
