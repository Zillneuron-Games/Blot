using System.IO;
using Blot.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Blot.Editor
{
    /// <summary>
    /// Menu: Blot ▶ Build Game Scene
    /// Creates Assets/Scenes/GameScene.unity from scratch, including all prefabs
    /// and wired Inspector references.  Run once; re-run to rebuild.
    /// </summary>
    public static class BuildGameScene
    {
        // ------------------------------------------------------------------ paths
        private const string ScenePath      = "Assets/Scenes/GameScene.unity";
        private const string PrefabsPath    = "Assets/Prefabs";
        private const string CardTexPath    = "Assets/Art/Textures/Cards";
        private const string BackCardPath   = "Assets/Art/Textures/Cards/BackCard1.png";

        // ------------------------------------------------------------------ entry
        [MenuItem("Blot/Build Game Scene")]
        public static void Build()
        {
            EnsureFolders();
            FixTextureImports();

            // New empty scene — all temp GameObjects for prefabs live here
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cardViewPrefab = CreateCardViewPrefab();
            var aiCardPrefab   = CreateAICardPrefab();

            PopulateScene(cardViewPrefab, aiCardPrefab);
            SetGameManagerExecutionOrder();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BuildGameScene] ✓  GameScene.unity saved to " + ScenePath);
        }

        // ================================================================== prefabs

        private static CardView CreateCardViewPrefab()
        {
            // Root — Button + Image (_cardImage) + CardView
            var root = new GameObject("CardView");
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 118);

            var img = root.AddComponent<Image>();
            img.color = Color.white;

            var backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackCardPath);
            if (backSprite != null) img.sprite = backSprite;

            root.AddComponent<Button>();
            var cv = root.AddComponent<CardView>();

            // Label child — bottom strip
            var label  = new GameObject("Label");
            label.transform.SetParent(root.transform, false);
            var lrt = label.AddComponent<RectTransform>();
            lrt.anchorMin        = new Vector2(0,   0);
            lrt.anchorMax        = new Vector2(1, 0.28f);
            lrt.offsetMin        = Vector2.zero;
            lrt.offsetMax        = Vector2.zero;
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Card";
            tmp.fontSize  = 10;
            tmp.color     = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;

            // Wire serialized fields
            Wire(cv, "_cardImage", img);
            Wire(cv, "_labelText", tmp);

            // Save prefab, destroy temp
            string path   = PrefabsPath + "/CardView.prefab";
            var    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Debug.Log("[BuildGameScene] CardView prefab → " + path);
            return prefab.GetComponent<CardView>();
        }

        private static Image CreateAICardPrefab()
        {
            var root = new GameObject("AICard");
            var rt   = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(52, 76);

            var img        = root.AddComponent<Image>();
            img.color      = Color.white;
            var backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackCardPath);
            if (backSprite != null) img.sprite = backSprite;

            string path   = PrefabsPath + "/AICard.prefab";
            var    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Debug.Log("[BuildGameScene] AICard prefab → " + path);
            return prefab.GetComponent<Image>();
        }

        // ================================================================== scene hierarchy

        private static void PopulateScene(CardView cardViewPrefab, Image aiCardPrefab)
        {
            // ---- GameManager -------------------------------------------------
            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<Blot.GameManager>();

            // ---- EventSystem -------------------------------------------------
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();

            // ---- Canvas ------------------------------------------------------
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var cv = canvasGO.transform; // shorthand

            // ---- Background --------------------------------------------------
            var bgGO  = UIChild(cv, "Background");
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.38f, 0.08f);
            StretchFull(bgGO);

            // ---- HUD (top-left) ----------------------------------------------
            // anchorMin/Max at top-left corner, pivot centred in widget
            var trumpLabel    = MakeLabel(cv, "TrumpLabel",    "Trump: —",              18, Color.yellow);
            var scoreLabel    = MakeLabel(cv, "ScoreLabel",    "TeamA 0  |  TeamB 0",  16, Color.white);
            var statusLabel   = MakeLabel(cv, "StatusLabel",   "Starting…",             15, Color.white);
            var trickInfoLabel = MakeLabel(cv, "TrickInfoLabel", "",                    13, new Color(0.9f, 0.9f, 0.9f));

            SetRect(trumpLabel,     AnchorTL, new Vector2(190,  -25), new Vector2(370, 28));
            SetRect(scoreLabel,     AnchorTL, new Vector2(190,  -58), new Vector2(370, 28));
            SetRect(statusLabel,    AnchorTL, new Vector2(190,  -91), new Vector2(370, 28));
            SetRect(trickInfoLabel, AnchorTL, new Vector2(190, -148), new Vector2(370, 90));
            trickInfoLabel.GetComponent<TMP_Text>().alignment    = TextAlignmentOptions.TopLeft;
            trickInfoLabel.GetComponent<TMP_Text>().overflowMode = TextOverflowModes.Overflow;

            // ---- AI Top — Player 2 CPU North (top-centre) --------------------
            var aiTopGO       = UIChild(cv, "AIHand_Top");
            var aiTopView     = aiTopGO.AddComponent<AIHandView>();
            var aiTopContainer = MakeContainer(aiTopGO.transform, "CardContainer", horizontal: true, spacing: -32f);
            SetRect(aiTopGO, AnchorTC, new Vector2(0, -82), new Vector2(640, 90));

            // ---- AI Left — Player 3 CPU West (left-centre) -------------------
            var aiLeftGO        = UIChild(cv, "AIHand_Left");
            var aiLeftView      = aiLeftGO.AddComponent<AIHandView>();
            var aiLeftContainer = MakeContainer(aiLeftGO.transform, "CardContainer", horizontal: false, spacing: -46f);
            SetRect(aiLeftGO, AnchorML, new Vector2(42, 0), new Vector2(76, 520));

            // ---- AI Right — Player 1 CPU East (right-centre) -----------------
            var aiRightGO        = UIChild(cv, "AIHand_Right");
            var aiRightView      = aiRightGO.AddComponent<AIHandView>();
            var aiRightContainer = MakeContainer(aiRightGO.transform, "CardContainer", horizontal: false, spacing: -46f);
            SetRect(aiRightGO, AnchorMR, new Vector2(-42, 0), new Vector2(76, 520));

            // ---- Trick Area (centre) -----------------------------------------
            var trickAreaGO   = UIChild(cv, "TrickArea");
            var trickAreaView = trickAreaGO.AddComponent<TrickAreaView>();
            SetRect(trickAreaGO, AnchorCC, new Vector2(0, 40), new Vector2(360, 320));

            // Cross-pattern slot positions (relative to TrickArea centre)
            var slot0 = MakeSlot(trickAreaGO.transform, "Slot_Player0", new Vector2(  0, -100)); // you   → bottom
            var slot1 = MakeSlot(trickAreaGO.transform, "Slot_Player1", new Vector2(130,    0)); // East  → right
            var slot2 = MakeSlot(trickAreaGO.transform, "Slot_Player2", new Vector2(  0,  100)); // North → top
            var slot3 = MakeSlot(trickAreaGO.transform, "Slot_Player3", new Vector2(-130,   0)); // West  → left

            // ---- Player Hand (bottom-centre) ---------------------------------
            var handGO      = UIChild(cv, "PlayerHand");
            var handView    = handGO.AddComponent<PlayerHandView>();
            var handContainer = MakeContainer(handGO.transform, "CardContainer", horizontal: true, spacing: 6f);
            SetRect(handGO, AnchorBC, new Vector2(0, 78), new Vector2(900, 130));

            // ---- End Panel (fullscreen overlay, starts hidden) ---------------
            var endGO = UIChild(cv, "EndPanel");
            StretchFull(endGO);
            var endBg = endGO.AddComponent<Image>();
            endBg.color = new Color(0, 0, 0, 0.78f);

            var resultLabel = MakeLabel(endGO.transform, "ResultLabel", "Game Over", 52, Color.white);
            SetRect(resultLabel, AnchorCC, new Vector2(0,  60), new Vector2(700, 80));
            resultLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

            var restartBtn = MakeButton(endGO.transform, "RestartButton", "Play Again");
            SetRect(restartBtn, AnchorCC, new Vector2(0, -30), new Vector2(220, 56));

            endGO.SetActive(false);

            // ---- GameUIManager -----------------------------------------------
            var guiGO  = new GameObject("GameUIManager");
            var guiMgr = guiGO.AddComponent<GameUIManager>();

            // ================================================================== wire references

            // GameUIManager
            Wire(guiMgr, "_trumpLabel",     trumpLabel.GetComponent<TMP_Text>());
            Wire(guiMgr, "_scoreLabel",     scoreLabel.GetComponent<TMP_Text>());
            Wire(guiMgr, "_trickInfoLabel", trickInfoLabel.GetComponent<TMP_Text>());
            Wire(guiMgr, "_statusLabel",    statusLabel.GetComponent<TMP_Text>());
            Wire(guiMgr, "_endPanel",       endGO);
            Wire(guiMgr, "_endResultLabel", resultLabel.GetComponent<TMP_Text>());
            Wire(guiMgr, "_restartButton",  restartBtn.GetComponent<Button>());
            Wire(guiMgr, "_humanHandView",  handView);

            // PlayerHandView
            Wire(handView, "_cardPrefab",   cardViewPrefab);
            Wire(handView, "_cardContainer", handContainer.transform);

            // TrickAreaView — Image[] array
            WireArray(trickAreaView, "_playerSlots", new Object[] { slot0, slot1, slot2, slot3 });

            // AIHandViews — prefab + container + playerId
            WireAIHand(aiTopView,   aiCardPrefab, aiTopContainer.transform,   2); // North
            WireAIHand(aiLeftView,  aiCardPrefab, aiLeftContainer.transform,  3); // West
            WireAIHand(aiRightView, aiCardPrefab, aiRightContainer.transform, 1); // East
        }

        // ================================================================== execution order

        private static void SetGameManagerExecutionOrder()
        {
            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == typeof(Blot.GameManager))
                {
                    MonoImporter.SetExecutionOrder(monoScript, 100);
                    Debug.Log("[BuildGameScene] GameManager execution order → 100");
                    return;
                }
            }
        }

        // ================================================================== folder / import helpers

        private static void EnsureFolders()
        {
            CreateFolder("Assets",        "Scenes");
            CreateFolder("Assets",        "Prefabs");
            CreateFolder("Assets",        "UI");
            CreateFolder("Assets",        "Editor");
        }

        private static void CreateFolder(string parent, string name)
        {
            string full = Path.Combine(parent, name);
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void FixTextureImports()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CardTexPath });
            foreach (var guid in guids)
            {
                var path     = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;

                importer.textureType     = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh();
            Debug.Log("[BuildGameScene] Card textures set to Sprite type.");
        }

        // ================================================================== serialized-field wiring

        private static void Wire(Object target, string field, Object value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[BuildGameScene] Field not found: {field} on {target.name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireInt(Object target, string field, int value)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[BuildGameScene] Field not found: {field} on {target.name}"); return; }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string field, Object[] values)
        {
            var so   = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[BuildGameScene] Array field not found: {field}"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireAIHand(AIHandView view, Image cardBackPrefab, Transform container, int playerId)
        {
            Wire(view,    "_cardBackPrefab", cardBackPrefab);
            Wire(view,    "_container",      container);
            WireInt(view, "_playerId",       playerId);
        }

        // ================================================================== UI factory helpers

        // Anchor presets (anchorMin == anchorMax → pivot-based positioning)
        private static readonly Vector2 AnchorTL = new(0,    1);
        private static readonly Vector2 AnchorTC = new(0.5f, 1);
        private static readonly Vector2 AnchorML = new(0,    0.5f);
        private static readonly Vector2 AnchorMR = new(1,    0.5f);
        private static readonly Vector2 AnchorBC = new(0.5f, 0);
        private static readonly Vector2 AnchorCC = new(0.5f, 0.5f);

        private static GameObject UIChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void SetRect(GameObject go, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject MakeLabel(Transform parent, string name, string text, float fontSize, Color color)
        {
            var go  = UIChild(parent, name);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Left;
            return go;
        }

        private static GameObject MakeButton(Transform parent, string name, string label)
        {
            var go  = UIChild(parent, name);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.55f, 0.15f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var lblGO  = UIChild(go.transform, "Label");
            var lblRT  = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero;
            lblRT.offsetMax = Vector2.zero;

            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 22;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static GameObject MakeContainer(Transform parent, string name, bool horizontal, float spacing)
        {
            var go = UIChild(parent, name);
            StretchFull(go);

            if (horizontal)
            {
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing              = spacing;
                hlg.childAlignment       = TextAnchor.MiddleCenter;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth    = false;
                hlg.childControlHeight   = false;
            }
            else
            {
                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.spacing              = spacing;
                vlg.childAlignment       = TextAnchor.MiddleCenter;
                vlg.childForceExpandWidth  = false;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth    = false;
                vlg.childControlHeight   = false;
            }
            return go;
        }

        /// <summary>Creates one trick-area card slot (hidden until a card is played).</summary>
        private static Image MakeSlot(Transform parent, string name, Vector2 localPos)
        {
            var go = UIChild(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = AnchorCC;
            rt.anchorMax        = AnchorCC;
            rt.pivot            = AnchorCC;
            rt.anchoredPosition = localPos;
            rt.sizeDelta        = new Vector2(72, 104);

            var img        = go.AddComponent<Image>();
            img.color      = Color.white;
            var baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardTexPath + "/BaseCard.png");
            if (baseSprite != null) img.sprite = baseSprite;

            go.SetActive(false);   // visible only when a card is played
            return img;
        }
    }
}
