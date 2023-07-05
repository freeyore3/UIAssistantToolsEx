using System.Diagnostics;
using System.Net.Mime;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine.U2D;
using Sirenix.Serialization;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.U2D;
#endif

public static class SpriteAtlasUtils 
{
    public static void FindAllSpriteAtlas(IList<SpriteAtlas> srpiteAtlasList)
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas");

        srpiteAtlasList.Clear();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            
            srpiteAtlasList.Add(atlas);
        }
    }
    public static SpriteAtlas GetSpriteAtlas(IList<SpriteAtlas> srpiteAtlasList, Sprite sprite)
    {
        foreach (var atlas in srpiteAtlasList)
        {
            if (atlas.CanBindTo(sprite))
            {
                return atlas;
            }
        }
        return null;
    }
}
