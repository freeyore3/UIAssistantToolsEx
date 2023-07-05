#if USE_UIPARTICLE
using Coffee.UIExtensions;
#endif
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using System.Collections.Generic;
using System.Linq;

public delegate SpriteAtlas GetSpriteAtlasCallback(Sprite sprite);

public class UIAssistantWindow : EditorWindow
{
    private int defaultDepth = UIAssistantTools.DefaultDepth;
    private int depthIdx = 1;
    private GameObject m_selectObj = null;
    private TreeNode m_treeRootNode = null;
    private Vector2 m_ScrollPos = Vector2.zero;

    void OnGUI()
    {
        GUILabelType();
        GUILayout.Label("UIAssistant");

        GUILabelType(TextAnchor.UpperLeft);
        GUILayout.Space(2);
        CreateSplit();

        Color saveColor = GUI.color;
        GUILayout.BeginHorizontal();
        for(int i = 0; i < UIAssistantTools.m_colors.Length; ++i) 
        {
            GUI.color = UIAssistantTools.m_colors[i];
            GUILayout.Label(string.Format("{0} : ", i));
            GUILayout.Box("", GUILayout.Width(15), GUILayout.Height(15));
            GUILayout.Space(8);
        }
        GUILayout.EndHorizontal();
        GUI.color = saveColor;

        GUILabelType(TextAnchor.UpperLeft);
        GUILayout.Space(2);
        CreateSplit();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Catch"))
        {
            Catch();
        }
        if (GUILayout.Button("Refresh"))
        {
            Refresh();
        }
        if (GUILayout.Button("Clear"))
        {
            Reset();
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("HierarychyOrder Name info:(Depth/MaterialID/TextureID) checkId BatchID");
        
        GUILabelType(TextAnchor.UpperLeft);
        GUILayout.Space(2);
        CreateSplit();

        ShowCatchUI();
    }

    private Vector2 scrollPosition;

    void ShowUsedSpriteAtlas()
    {
        HashSet<SpriteAtlas> spriteAtlasSet = new HashSet<SpriteAtlas>();
        GetUsedSpriteAtlases(spriteAtlasSet, m_treeRootNode);
        
        GUILayout.Label($"Used SpriteAtlas (Count = {spriteAtlasSet.Count}):");
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.MaxHeight(100));
        GUILayout.BeginVertical();
        GUILayout.Space(2);
        foreach (var spriteAtlas in spriteAtlasSet)
        {
            GUILayout.BeginHorizontal();
            Texture icon = EditorGUIUtility.ObjectContent(spriteAtlas, spriteAtlas.GetType()).image;
            GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
            
            GUILayout.Label(spriteAtlas.name);
            
            Rect rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                Selection.activeObject = spriteAtlas;
            }
            
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(2);
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }
    private void ShowCatchUI() 
    {
        if (m_selectObj != null) 
        {
            GUILabelType(TextAnchor.UpperLeft);
            GUILayout.Space(2);
            GUILayout.Label(m_treeRootNode == null ? "Result: " : string.Format("Result: batchCount={0} (maskBatchCount= {1})", m_treeRootNode.batchCount, m_treeRootNode.maskBatchCount));
            GUILayout.Space(2);

            ShowUsedSpriteAtlas();
            
            // get layout rect
            // Rect rect = GUILayoutUtility.GetRect(0, 100000, 30, 100000);
            // Debug.Log(rect);
            // GUILayout.Box("", GUILayout.Width(rect.width), GUILayout.Height(rect.height));
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Rect rect = GUILayoutUtility.GetLastRect();
        
            //m_ScrollPos = GUI.BeginScrollView(new Rect(10, 170, 800, position.height - 160), m_ScrollPos, new Rect(0, 0, m_treeRootNode.RecursiveSize.x, m_treeRootNode.RecursiveSize.y), true, true);
            m_ScrollPos = GUI.BeginScrollView(rect, m_ScrollPos, new Rect(0, 0, m_treeRootNode.RecursiveSize.x, m_treeRootNode.RecursiveSize.y), false, false);
            m_treeRootNode.OnGUI();
            GUI.EndScrollView();
            
            
        }
    }

    private void Catch()
    {
        if(Selection.activeGameObject == null) 
        {
            EditorUtility.DisplayDialog("Tips", "Select Object is null!", "close");
            return;
        }

        if (Selection.activeGameObject.layer == LayerMask.NameToLayer("UI")) 
        {
            m_selectObj = Selection.activeGameObject;
            Refresh();
        }
    }

    List<SpriteAtlas> srpiteAtlasList = new List<SpriteAtlas>();

    SpriteAtlas GetSpriteAtlas(Sprite sprite)
    {
        var spriteAtlas = SpriteAtlasUtils.GetSpriteAtlas(srpiteAtlasList, sprite);
        return spriteAtlas;
    }

    void GetUsedSpriteAtlases(HashSet<SpriteAtlas> spriteAtlasSet, TreeNode parentNode)
    {
        var nodesInfo = parentNode.GetNodesInfo();
        foreach (var nodeInfo in nodesInfo)
        {
            var spriteAtlas = nodeInfo.Value.SpriteAtlas;
            if (spriteAtlas != null)
            {
                if (!spriteAtlasSet.Contains(spriteAtlas))
                {
                    spriteAtlasSet.Add(spriteAtlas);
                }
            }
        }
    }
    private void Refresh()
    {
        if (m_treeRootNode != null)
        {
            m_treeRootNode.Destroy();
        }

        SpriteAtlasUtils.FindAllSpriteAtlas(srpiteAtlasList);
        
        if (m_selectObj != null)
        {
            depthIdx = 1;
            m_treeRootNode = new TreeNode(m_selectObj.name, m_selectObj, GetSpriteAtlas, depthIdx * defaultDepth);
            m_treeRootNode.IsRoot = true;
            GenChildNodes(m_treeRootNode, m_selectObj.transform);
            UIAssistantTools.GenTreeInfo(m_treeRootNode);
        }
    }

    private void GenChildNodes(TreeNode node, Transform transform)
    {
        if(transform.childCount > 0) 
        {
            int depth = 0;
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                if(child.gameObject.activeSelf) 
                {
                    depth = node.Depth + 1;
                    if (child.GetComponent<Canvas>() != null) 
                    {
                        ++depthIdx;
                        depth = depthIdx * defaultDepth;
                    }
#if USE_UIPARTICLE
                    if (child.GetComponent<UIParticle>() != null)
                    {
                        continue;
                    }
#endif
                    TreeNode childNode = new TreeNode(child.name, child.gameObject, GetSpriteAtlas, depth);
                    GenChildNodes(childNode, child);
                    node.AddChild(childNode);
                }
            }
        }
    }

    private void Reset()
    {
        m_selectObj = null;
        m_treeRootNode = null;
        m_ScrollPos = Vector2.zero;
    }

    public GUIStyle GUILabelType(TextAnchor anchor = TextAnchor.UpperCenter)
    {
        GUIStyle labelstyle = GUI.skin.GetStyle("Label");
        labelstyle.alignment = anchor;
        return labelstyle;
    }

    public void CreateSplit()
    {
        GUILayout.Label("-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
    }
}