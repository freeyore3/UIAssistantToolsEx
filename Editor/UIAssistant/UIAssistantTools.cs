using System.Collections.Generic;
using UnityEngine;

public class UIAssistantTools
{
    public static Color[] m_colors = new Color[]
    {
        new Color(250 / 255f,44 / 255f,123 / 255f),
        new Color(255 / 255f,56 / 255f,224 / 255f),
        new Color(255 / 255f,162 / 255f,53 / 255f),
        new Color(4 / 255f,197 / 255f,243 / 255f),
        new Color(0 / 255f,102 / 255f,254 / 255f),
        new Color(137 / 255f,50 / 255f,165 / 255f),
        new Color(201 / 255f,4 / 255f,68 / 255f),
        new Color(203 / 255f,155 / 255f,255 / 255f),
        new Color(128 / 255f,128 / 255f,128 / 255f),
        new Color(144 / 255f,237 / 255f,125 / 255f),
        new Color(247 / 255f,163 / 255f,92 / 255f),
        new Color(128 / 255f,133 / 255f,233 / 255f),
    };
    private static GUIStyle[] styles = null;
    private static List<DrawRectInfo> list = new List<DrawRectInfo>();
    private static Dictionary<int, bool> maskDic = new Dictionary<int, bool>();
    
    public static int DefaultDepth = 1000;

    public static void InitStyles() 
    {
        styles = new GUIStyle[m_colors.Length];
        for (int i = 0; i < styles.Length; ++i) 
        {
            styles[i] = new GUIStyle();
            styles[i].normal.textColor = m_colors[i];
        }
    }

    public static GUIStyle GetStyles(int id) 
    {
        if(styles == null) 
        {
            InitStyles();
        }

        return styles[id % m_colors.Length];
    }

    public static void GenTreeInfo(TreeNode node)
    {
        if (node.IsRoot) 
        {
            Dictionary<int, KeyValuePair<TreeNode, List<DrawRectInfo>>> rectInfos = new Dictionary<int, KeyValuePair<TreeNode, List<DrawRectInfo>>>();
            rectInfos.Add(1, new KeyValuePair<TreeNode, List<DrawRectInfo>>(node, new List<DrawRectInfo>()));
            List<KeyValuePair<TreeNode, UINodeInfo>> infos = node.GetNodesInfo();
            int idx = 0;
            for(int i = 0; i < infos.Count; ++i) 
            {
                if (infos[i].Value.Use) 
                {
                    DrawRectInfo rectInfo = new DrawRectInfo(infos[i].Value.Name, infos[i].Value.IsOnlyCanvas);
                    rectInfo.Depth = infos[i].Value.Depth;
                    rectInfo.HierarychyOrder = idx++;
                    rectInfo.MaterialInstanceID = infos[i].Value.MaterialInstanceID;
                    rectInfo.TextureID = infos[i].Value.TextureID;
                    rectInfo.Corners = infos[i].Value.Corners;
                    rectInfo.IsInMask = infos[i].Value.IsInMask;
                    rectInfo.IsInMask2D = infos[i].Value.IsInMask2D;
                    infos[i].Value.ConnectInfo(rectInfo);
                    if (!rectInfos.ContainsKey(rectInfo.Depth / DefaultDepth)) 
                    {
                        rectInfos[rectInfo.Depth / DefaultDepth] = new KeyValuePair<TreeNode, List<DrawRectInfo>>(infos[i].Key, new List<DrawRectInfo>());
                    }
                    rectInfos[rectInfo.Depth / DefaultDepth].Value.Add(rectInfo);
                }
            }

            if(rectInfos.Count > 0) 
            {
                int allBatchCount = 0;
                int allMaskBatchCount = 0;
                KeyValuePair<int, int> temp;
                foreach(var keyValue in rectInfos) 
                {
                    temp = CalculateDepthInCanvas(keyValue.Value.Key, keyValue.Value.Value);
                    allBatchCount += temp.Key;
                    allMaskBatchCount += temp.Value;
                }
                node.batchCount = allBatchCount;
                node.maskBatchCount = allMaskBatchCount;
            }
        }
    }

    private static KeyValuePair<int, int> CalculateDepthInCanvas(TreeNode node, List<DrawRectInfo> rectInfos) 
    {
        if(rectInfos.Count == 0) 
        {
            return new KeyValuePair<int, int>(0, 0);
        }
        list.Clear();
        list.Add(rectInfos[0]);

        for (int i = 1; i < rectInfos.Count; ++i)
        {
            CalculateDepth(rectInfos[i]);
            list.Add(rectInfos[i]);
        }

        //Print
        rectInfos.Sort();

        rectInfos[0].BatchID = rectInfos[0].Depth;
        for (int i = 1; i < rectInfos.Count; ++i)
        {
            if ((rectInfos[i].Depth == rectInfos[i - 1].Depth && rectInfos[i].CanBatch(rectInfos[i - 1])) || rectInfos[i].IsOnlyCanvas)
            {
                rectInfos[i].BatchID = rectInfos[i - 1].BatchID;
            }
            else
            {
                rectInfos[i].BatchID = rectInfos[i - 1].BatchID + 1;
            }
            //rectInfos[i].Print();
        }

        for (int i = 0; i < rectInfos.Count - 2; ++i)
        {
            for (int j = i + 1; j < rectInfos.Count; ++j)
            {
                if (rectInfos[i].BatchID != rectInfos[j].BatchID && rectInfos[i].CanBatch(rectInfos[j]))
                {
                    rectInfos[i].check = rectInfos[j].HierarychyOrder;
                    rectInfos[j].check = rectInfos[i].HierarychyOrder;
                }
            }
        }

        maskDic.Clear();
        for (int i = 0; i < rectInfos.Count; ++i) 
        {
            if(rectInfos[i].IsInMask == 0 && !maskDic.ContainsKey(rectInfos[i].BatchID)) 
            {
                maskDic.Add(rectInfos[i].BatchID, true);
            }
        }

        node.maskBatchCount = maskDic.Count;
        node.batchCount = rectInfos[rectInfos.Count - 1].BatchID - rectInfos[0].Depth + 1 + maskDic.Count;
        node.UpdataInfo();
        DrawRectLine ui = node.AssetObject.GetComponent<DrawRectLine>();
        if (ui == null)
        {
            ui = node.AssetObject.AddComponent<DrawRectLine>();
        }
        ui.SetDrawRectInfoList(rectInfos);
        return new KeyValuePair<int, int>(node.batchCount, node.maskBatchCount);
    }

    private static void CalculateDepth(DrawRectInfo a) 
    {
        a.Depth = list[0].Depth;
        for(int i = 0; i < list.Count; ++i) 
        {
            if(Intersects(a.GetRect(), list[i].GetRect())) 
            {
                int depth = list[i].Depth + 1;
                if (list[i].CanBatch(a)) 
                {
                    depth = list[i].Depth;
                }
                a.Depth = depth > a.Depth ? depth : a.Depth;
            }
        }
    }

    private static bool Intersects(Rect a, Rect b) 
    {
        if(a.xMax > b.xMin && b.xMax > a.xMin && a.yMax > b.yMin && b.yMax > a.yMin) 
        {
            return true;
        }

        return false;
    }
}