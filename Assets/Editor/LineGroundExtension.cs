using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LineGround))]
public class LineGroundExtension : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LineGround lineGround = (LineGround)target;
        if(GUILayout.Button("�� �����"))
        {
            lineGround.CreateGround();
        }

        if(GUILayout.Button("�� �߰�"))
        {
            lineGround.points.Add(lineGround.gameObject.transform.position);
        }

        if(GUILayout.Button("������ �� ����"))
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
