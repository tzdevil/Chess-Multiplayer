using UnityEditor;
using UnityEngine;

namespace tzdevil.LabelBlocks
{
    [InitializeOnLoad]
    public class LabelBlocks : MonoBehaviour
    {
        static LabelBlocks()
        {
            // Evente subscribe oluyoruz, normal olay.
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            // bu *tahminimce* her hierarchy objesinde �al���yor, '�u anki'ni al�yor de�i�iklik yapaca�� objeyi.
            var obj = EditorUtility.InstanceIDToObject(instanceID);

            // e�er bir obje varsa?
            if (obj != null)
            {
                // obj.name'i cache'ledim.
                var objName = obj.name;

                // burada kendi algoritmam� yazd�m.
                // dedim ki, e�er bir objenin ismi ** ile ba�l�yorsa onu label'a d�n��t�r (renkli kutucuk)
                if (objName[0] == '-' && objName[1] == '-')
                {
                    // font color k�sm�, sadece caching (hatta bunu �ste bile koyabilirim lol)
                    var fontColor = Color.white;
                    // e�er ismin ���NC� karakteri r, g, b ise ona g�re renk ver, # ise ba�l�yorsa hex kodunu al; de�ilse (default) Color.gray olsun}
                    var backgroundColor = objName[2] switch
                    {
                        'r' => Color.red,
                        'g' => Color.green,
                        'b' => Color.blue,
                        '#' when ColorUtility.TryParseHtmlString(objName[2..9], out Color col) => col,
                        _ => Color.gray
                    };

                    // labeli �iz
                    Rect offsetRect = new(selectionRect.position, selectionRect.size);
                    EditorGUI.DrawRect(selectionRect, backgroundColor);

                    // objName'i Substringle, 3'ten sonraki b�t�n harfleri al�p yeni string yap.
                    var newobjName = objName[(objName[2] != '#' ? 3 : 9)..];
                    EditorGUI.LabelField(offsetRect, newobjName, new GUIStyle()
                    {
                        // normal'in i�inde farkl� �eyler de var, bak�n�rs�n.
                        normal = new GUIStyleState() { textColor = fontColor },
                        // richText html yapmam�z� sa�l�yor yani <color=#fade55>Hello World</color> yap�nca hierarchy objesi sar� oluyor.
                        richText = true,
                        // UpperCenter ��nk� niyeyse 'orta' o, MiddleCenter a�a�� do�ru g�steriyor biraz :(
                        alignment = TextAnchor.UpperCenter
                        // GUISTyle() i�inde farkl� �eyler de var, son line'a virg�l at�p bakars�n.
                    });
                }
            }
        }
    }
}