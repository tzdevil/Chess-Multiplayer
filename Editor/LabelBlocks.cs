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
            // bu *tahminimce* her hierarchy objesinde çalýþýyor, 'þu anki'ni alýyor deðiþiklik yapacaðý objeyi.
            var obj = EditorUtility.InstanceIDToObject(instanceID);

            // eðer bir obje varsa?
            if (obj != null)
            {
                // obj.name'i cache'ledim.
                var objName = obj.name;

                // burada kendi algoritmamý yazdým.
                // dedim ki, eðer bir objenin ismi ** ile baþlýyorsa onu label'a dönüþtür (renkli kutucuk)
                if (objName[0] == '-' && objName[1] == '-')
                {
                    // font color kýsmý, sadece caching (hatta bunu üste bile koyabilirim lol)
                    var fontColor = Color.white;
                    // eðer ismin ÜÇÜNCÜ karakteri r, g, b ise ona göre renk ver, # ise baþlýyorsa hex kodunu al; deðilse (default) Color.gray olsun}
                    var backgroundColor = objName[2] switch
                    {
                        'r' => Color.red,
                        'g' => Color.green,
                        'b' => Color.blue,
                        '#' when ColorUtility.TryParseHtmlString(objName[2..9], out Color col) => col,
                        _ => Color.gray
                    };

                    // labeli çiz
                    Rect offsetRect = new(selectionRect.position, selectionRect.size);
                    EditorGUI.DrawRect(selectionRect, backgroundColor);

                    // objName'i Substringle, 3'ten sonraki bütün harfleri alýp yeni string yap.
                    var newobjName = objName[(objName[2] != '#' ? 3 : 9)..];
                    EditorGUI.LabelField(offsetRect, newobjName, new GUIStyle()
                    {
                        // normal'in içinde farklý þeyler de var, bakýnýrsýn.
                        normal = new GUIStyleState() { textColor = fontColor },
                        // richText html yapmamýzý saðlýyor yani <color=#fade55>Hello World</color> yapýnca hierarchy objesi sarý oluyor.
                        richText = true,
                        // UpperCenter çünkü niyeyse 'orta' o, MiddleCenter aþaðý doðru gösteriyor biraz :(
                        alignment = TextAnchor.UpperCenter
                        // GUISTyle() içinde farklý þeyler de var, son line'a virgül atýp bakarsýn.
                    });
                }
            }
        }
    }
}