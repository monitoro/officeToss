using UnityEngine;
using UnityEditor;
using UnityEngine.UI; // Legacy Text
using TMPro; // TextMeshPro

public class BatchTextReplacer : EditorWindow
{
    // 텍스트 교체용
    private string findText = "";
    private string replaceText = "";

    // 폰트 교체용
    private bool replaceFont = false;
    private Font newLegacyFont;
    private TMP_FontAsset newTmpFont;

    private int modifiedCount = 0;

    [MenuItem("Tools/Batch Text Replacer")]
    public static void ShowWindow()
    {
        GetWindow<BatchTextReplacer>("Batch Text Replacer");
    }

    void OnGUI()
    {
        GUILayout.Label("Find and Replace Text", EditorStyles.boldLabel);
        findText = EditorGUILayout.TextField("Find Text:", findText);
        replaceText = EditorGUILayout.TextField("Replace With:", replaceText);

        EditorGUILayout.Space(10); // 여백

        // --- 폰트 교체 섹션 ---
        GUILayout.Label("Font Replacement (Optional)", EditorStyles.boldLabel);
        replaceFont = EditorGUILayout.Toggle("Change Font?", replaceFont);

        // 폰트 변경 토글이 켜져 있을 때만 폰트 지정 필드를 보여줍니다.
        if (replaceFont)
        {
            newLegacyFont = (Font)EditorGUILayout.ObjectField("New Legacy Font", newLegacyFont, typeof(Font), false);
            newTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New TMP Font Asset", newTmpFont, typeof(TMP_FontAsset), false);
        }
        // --- 폰트 교체 섹션 끝 ---

        EditorGUILayout.Space(10); // 여백

        if (GUILayout.Button("Execute Batch Operation in Current Scene"))
        {
            // 실행할 작업이 있는지 확인
            bool doTextReplace = !string.IsNullOrEmpty(findText);
            if (!doTextReplace && !replaceFont)
            {
                Debug.LogWarning("실행할 작업이 없습니다. 텍스트를 입력하거나 'Change Font?'를 체크하세요.");
                return;
            }

            // 폰트 변경이 켜졌는데 폰트가 할당되지 않았는지 체크
            if (replaceFont && newLegacyFont == null && newTmpFont == null)
            {
                Debug.LogWarning("폰트 변경이 활성화되었지만, 할당된 폰트가 없습니다.");
                return;
            }

            modifiedCount = 0;

            // 씬의 모든 관련 컴포넌트를 찾아 작업 실행
            ReplaceInLegacyText(doTextReplace);
            ReplaceInTMP_UGUI(doTextReplace);
            ReplaceInTMP_3D(doTextReplace);

            Debug.Log($"[BatchTextReplacer] 작업 완료. 총 {modifiedCount}개의 오브젝트를 수정했습니다.");
        }
    }

    // Legacy Text (UnityEngine.UI.Text)
    private void ReplaceInLegacyText(bool doTextReplace)
    {
        Text[] legacyTexts = FindObjectsByType<Text>(FindObjectsSortMode.None);
        foreach (Text t in legacyTexts)
        {
            bool modified = false;
            bool textFound = doTextReplace && t.text.Contains(findText);

            // 1. 텍스트 교체 (텍스트 찾기 옵션이 켜져 있고, 텍스트를 찾았을 때)
            if (textFound)
            {
                Undo.RecordObject(t, "Replace Text");
                t.text = t.text.Replace(findText, replaceText);
                modified = true;
            }

            // 2. 폰트 교체
            // (조건: 폰트 교체 옵션이 켜져있고, 새 폰트가 할당됐고, 현재 폰트가 새 폰트와 다를 때)
            // (또한, '텍스트 찾기'가 비활성화(씬 전체)이거나, '텍스트 찾기'가 활성화됐고 텍스트를 찾았을 때만)
            if (replaceFont && newLegacyFont != null && t.font != newLegacyFont)
            {
                if (!doTextReplace || (doTextReplace && textFound))
                {
                    Undo.RecordObject(t, "Replace Font");
                    t.font = newLegacyFont;
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(t);
                modifiedCount++;
            }
        }
    }

    // TextMeshPro UGUI (TMPro.TextMeshProUGUI)
    private void ReplaceInTMP_UGUI(bool doTextReplace)
    {
        TextMeshProUGUI[] tmpUguis = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (TextMeshProUGUI t in tmpUguis)
        {
            bool modified = false;
            bool textFound = doTextReplace && t.text.Contains(findText);

            // 1. 텍스트 교체
            if (textFound)
            {
                Undo.RecordObject(t, "Replace Text");
                t.text = t.text.Replace(findText, replaceText);
                modified = true;
            }

            // 2. 폰트 교체
            if (replaceFont && newTmpFont != null && t.font != newTmpFont)
            {
                if (!doTextReplace || (doTextReplace && textFound))
                {
                    Undo.RecordObject(t, "Replace Font");
                    t.font = newTmpFont; // TMP는 font 프로퍼티로 폰트 에셋을 사용합니다.
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(t);
                modifiedCount++;
            }
        }
    }

    // TextMeshPro 3D (TMPro.TextMeshPro)
    private void ReplaceInTMP_3D(bool doTextReplace)
    {
        TextMeshPro[] tmp3Ds = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
        foreach (TextMeshPro t in tmp3Ds)
        {
            bool modified = false;
            bool textFound = doTextReplace && t.text.Contains(findText);

            // 1. 텍스트 교체
            if (textFound)
            {
                Undo.RecordObject(t, "Replace Text");
                t.text = t.text.Replace(findText, replaceText);
                modified = true;
            }

            // 2. 폰트 교체
            if (replaceFont && newTmpFont != null && t.font != newTmpFont)
            {
                if (!doTextReplace || (doTextReplace && textFound))
                {
                    Undo.RecordObject(t, "Replace Font");
                    t.font = newTmpFont;
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(t);
                modifiedCount++;
            }
        }
    }
}