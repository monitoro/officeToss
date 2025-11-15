using UnityEngine;
using UnityEditor;
using UnityEngine.UI; // Legacy Text
using TMPro; // TextMeshPro
using System.Collections.Generic;
using System.Linq;

public class SequentialTextLocalizer : EditorWindow
{
    // 폰트 설정
    private Font newLegacyFont;
    private TMP_FontAsset newTmpFont;

    // 텍스트 목록 및 현재 진행 상태
    private List<Component> textComponents = new List<Component>();
    private int currentIndex = -1;
    private string newTextValue = ""; // 현재 수정 중인 텍스트

    // GUI용 스크롤
    private Vector2 originalTextScrollPos;

    [MenuItem("Tools/Sequential Text Localizer")]
    public static void ShowWindow()
    {
        GetWindow<SequentialTextLocalizer>("Sequential Localizer");
    }

    void OnGUI()
    {
        GUILayout.Label("Sequential Text & Font Localizer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // --- 1. 폰트 설정 ---
        GUILayout.Label("적용할 폰트 (선택 사항)", EditorStyles.boldLabel);
        newLegacyFont = (Font)EditorGUILayout.ObjectField("New Legacy Font", newLegacyFont, typeof(Font), false);
        newTmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New TMP Font Asset", newTmpFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(10);

        // --- 2. 텍스트 검색 ---
        if (GUILayout.Button("1. 씬에서 모든 텍스트 찾기", GUILayout.Height(30)))
        {
            FindAllTextsInScene();
        }

        EditorGUILayout.Space(10);

        // 텍스트를 찾은 후에만 아래 UI 표시
        if (currentIndex == -1 || textComponents.Count == 0)
        {
            GUILayout.Label("버튼을 눌러 씬의 텍스트를 검색하세요.");
            return;
        }

        // --- 3. 진행 상황 ---
        GUILayout.Label($"진행 상황: {currentIndex + 1} / {textComponents.Count}", EditorStyles.boldLabel);

        // 현재 작업 완료 시
        if (currentIndex >= textComponents.Count)
        {
            GUILayout.Label("모든 텍스트를 검토했습니다!");
            if (GUILayout.Button("처음부터 다시 시작"))
            {
                FindAllTextsInScene();
            }
            return;
        }

        // --- 4. 현재 텍스트 정보 ---
        Component currentComponent = textComponents[currentIndex];
        GameObject currentGO = currentComponent.gameObject;

        // 현재 오브젝트 필드 (클릭 시 Hierarchy에서 하이라이트됨)
        EditorGUILayout.ObjectField("Current Object", currentGO, typeof(GameObject), true);

        // 원본 텍스트 표시 (스크롤 가능)
        GUILayout.Label("Original Text:");
        originalTextScrollPos = EditorGUILayout.BeginScrollView(originalTextScrollPos, GUILayout.MinHeight(80), GUILayout.MaxHeight(150));
        EditorGUILayout.SelectableLabel(GetTextFromComponent(currentComponent), EditorStyles.textArea, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // --- 5. 새 텍스트 입력 ---
        GUILayout.Label("New Text (수정할 내용):");
        newTextValue = EditorGUILayout.TextField(newTextValue);

        EditorGUILayout.Space(10);

        // --- 6. 제어 버튼 ---
        EditorGUILayout.BeginHorizontal();

        // 이전
        if (GUILayout.Button("<< Previous"))
        {
            MovePrevious();
        }

        // 건너뛰기
        if (GUILayout.Button("Skip & Next >>"))
        {
            MoveNext();
        }

        // 적용
        if (GUILayout.Button("Apply & Next >>", GUILayout.Height(30)))
        {
            ApplyChangesAndMoveNext();
        }

        EditorGUILayout.EndHorizontal();
    }

    // 씬에서 모든 텍스트 컴포넌트를 찾아 리스트에 저장
    private void FindAllTextsInScene()
    {
        textComponents.Clear();

        // 정렬 순서를 이름순(InstanceID)으로 하여 항상 동일한 순서를 보장
        var legacyTexts = FindObjectsByType<Text>(FindObjectsSortMode.InstanceID).Where(t => t.gameObject.activeInHierarchy);
        var tmpUguis = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.InstanceID).Where(t => t.gameObject.activeInHierarchy);
        var tmp3Ds = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.InstanceID).Where(t => t.gameObject.activeInHierarchy);

        textComponents.AddRange(legacyTexts.Cast<Component>());
        textComponents.AddRange(tmpUguis.Cast<Component>());
        textComponents.AddRange(tmp3Ds.Cast<Component>());

        // 이름 순으로 한 번 더 정렬
        textComponents = textComponents.OrderBy(c => c.gameObject.name).ToList();

        if (textComponents.Count > 0)
        {
            currentIndex = 0;
            LoadTextToEditor(currentIndex);
            Debug.Log($"[Localizer] 총 {textComponents.Count}개의 텍스트를 찾았습니다.");
        }
        else
        {
            currentIndex = -1;
            Debug.LogWarning("[Localizer] 씬에서 텍스트 컴포넌트를 찾지 못했습니다.");
        }
    }

    // 현재 인덱스의 텍스트를 'New Text' 입력창에 불러오기
    private void LoadTextToEditor(int index)
    {
        if (index < 0 || index >= textComponents.Count) return;

        Component comp = textComponents[index];
        newTextValue = GetTextFromComponent(comp); // 원본 텍스트를 입력창에 미리 채움

        // Hierarchy와 Scene 뷰에서 해당 오브젝트를 하이라이트
        EditorGUIUtility.PingObject(comp.gameObject);
        Selection.activeGameObject = comp.gameObject;
    }

    // 컴포넌트 타입에 맞춰 텍스트 가져오기
    private string GetTextFromComponent(Component c)
    {
        if (c is Text legacy) return legacy.text;
        if (c is TextMeshProUGUI tmpUGUI) return tmpUGUI.text;
        if (c is TextMeshPro tmp3D) return tmp3D.text;
        return "";
    }

    // 텍스트와 폰트 적용
    private void ApplyChangesAndMoveNext()
    {
        if (currentIndex < 0 || currentIndex >= textComponents.Count) return;

        Component comp = textComponents[currentIndex];

        // Undo(Ctrl+Z) 기록
        Undo.RecordObject(comp, "Localize Text and Font");

        bool modified = false;

        // 1. 텍스트 적용
        string originalText = GetTextFromComponent(comp);
        if (originalText != newTextValue)
        {
            if (comp is Text legacy) legacy.text = newTextValue;
            else if (comp is TextMeshProUGUI tmpUGUI) tmpUGUI.text = newTextValue;
            else if (comp is TextMeshPro tmp3D) tmp3D.text = newTextValue;
            modified = true;
        }

        // 2. 폰트 적용 (설정된 경우)
        if (comp is Text legacyFont && newLegacyFont != null && legacyFont.font != newLegacyFont)
        {
            legacyFont.font = newLegacyFont;
            modified = true;
        }
        else if (comp is TextMeshProUGUI tmpUGUIFont && newTmpFont != null && tmpUGUIFont.font != newTmpFont)
        {
            tmpUGUIFont.font = newTmpFont;
            modified = true;
        }
        else if (comp is TextMeshPro tmp3DFont && newTmpFont != null && tmp3DFont.font != newTmpFont)
        {
            tmp3DFont.font = newTmpFont;
            modified = true;
        }

        if (modified)
        {
            // 변경 사항을 씬에 저장
            EditorUtility.SetDirty(comp);
        }

        MoveNext();
    }

    // 다음 텍스트로 이동
    private void MoveNext()
    {
        currentIndex++;
        if (currentIndex < textComponents.Count)
        {
            LoadTextToEditor(currentIndex);
        }
        else
        {
            newTextValue = ""; // 작업 완료 시 입력창 비우기
        }
    }

    // 이전 텍스트로 이동
    private void MovePrevious()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            LoadTextToEditor(currentIndex);
        }
    }
}