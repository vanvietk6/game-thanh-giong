using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ThanhGiong
{
    /// <summary>
    /// Toàn bộ giao diện (uGUI) được dựng bằng code: HUD, menu chính, cốt truyện, chọn nâng cấp,
    /// tạm dừng, kết thúc, hướng dẫn, chữ bay (số sát thương) và banner thông báo.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        static readonly Color Gold = new(1f, 0.82f, 0.35f);
        static readonly Color PanelBg = new(0f, 0f, 0f, 0.55f);
        static readonly Color ButtonNormal = new(0.45f, 0.18f, 0.08f, 0.95f);
        static readonly Color ButtonHover = new(0.70f, 0.32f, 0.10f, 1f);

        static readonly string[] StorySlides =
        {
            "Đời Hùng Vương thứ sáu, ở làng Phù Đổng có hai vợ chồng ông lão sinh được một cậu con trai.\nLạ thay, cậu bé lên ba mà vẫn không biết nói, biết cười, đặt đâu nằm đấy.",
            "Bấy giờ giặc Ân kéo đến xâm phạm bờ cõi, thế giặc rất mạnh.\nNhà vua sai sứ giả đi khắp nơi tìm người tài giỏi cứu nước.",
            "Nghe tiếng rao, cậu bé bỗng cất tiếng nói:\n\"Ông về tâu với vua sắm cho ta một con ngựa sắt, một cái roi sắt và một tấm áo giáp sắt,\nta sẽ phá tan lũ giặc này!\"",
            "Từ hôm đó, cậu bé lớn nhanh như thổi. Cả làng góp gạo nuôi cậu.\nKhi sứ giả mang ngựa sắt, roi sắt, áo giáp sắt đến, cậu vươn vai thành tráng sĩ oai phong lẫm liệt.",
            "Ngựa sắt hí vang, phun lửa!\nHãy cùng Thánh Gióng đánh tan giặc Ân, bảo vệ làng Phù Đổng!"
        };

        const string HelpText =
            "<color=#FFD15A><b>ĐIỀU KHIỂN</b></color>\n" +
            "W A S D / Phím mũi tên — Di chuyển ngựa sắt\n" +
            "J / Chuột trái — Vung roi sắt (3 đòn liên hoàn, đòn thứ 3 quét rộng)\n" +
            "Space / Shift — Lướt nhanh né đòn (không bị trúng đòn khi lướt)\n" +
            "K / Chuột phải — Ngựa sắt phun lửa (tốn năng lượng Lửa)\n" +
            "E — Nhổ bụi tre ngà làm vũ khí (đánh xa, quét rộng)\n" +
            "Esc / P — Tạm dừng\n" +
            "Tay cầm: Cần trái · X đánh · A lướt · Y phun lửa · B nhổ tre\n\n" +
            "<color=#FFD15A><b>MẸO</b></color>\n" +
            "• Đánh trúng giặc sẽ hồi năng lượng Lửa.\n" +
            "• Dấu \"!\" đỏ trên đầu giặc: chúng sắp ra đòn — đánh trúng lúc đó sẽ làm chúng mất đòn.\n" +
            "• Lính khiên đỡ được đòn phía trước — hãy đánh sau lưng hoặc phun lửa.\n" +
            "• Nhặt nắm cơm (rơi ra từ giặc) để hồi máu.\n" +
            "• Mỗi đợt thắng, Gióng lớn thêm và được chọn một sức mạnh mới.\n" +
            "• Vùng đỏ dưới đất là đòn sắp đánh của tướng giặc — tránh ra ngay!";

        class FloatingText
        {
            public RectTransform rt;
            public Text text;
            public Vector3 world;
            public float life, maxLife;
        }

        Font font;
        RectTransform canvasRect, floatRoot;
        GameObject hud, menu, story, upgrade, pause, endPanel, help, bossBar;
        RectTransform hpFill, energyFill, bossFill;
        Text hpText, energyText, growthText, waveText, scoreText, promptText, bambooText, bossName;
        Text storyText, menuHigh, endTitle, endBody, bannerTitle, bannerSub;
        CanvasGroup bannerGroup;
        Image hurtOverlay;
        readonly List<Text> upgradeTitles = new(), upgradeDescs = new();
        readonly List<FloatingText> floats = new();
        readonly Stack<FloatingText> floatPool = new();
        float bannerTimer, bannerDuration, hurtAlpha;
        int storyIndex;

        public bool EndPanelShown { get; private set; }
        public bool HelpOpen => help.activeSelf;

        static GameManager GM => GameManager.I;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
            BuildHud();
            floatRoot = Stretch("ChuBay", canvasRect);
            BuildBanner();
            BuildMenu();
            BuildStory();
            BuildUpgrade();
            BuildPause();
            BuildEnd();
            BuildHelp();
            HideAll();
        }

        // ------------------------------------------------------------------ Tiện ích dựng UI
        void BuildCanvas()
        {
            var go = new GameObject("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            canvasRect = go.GetComponent<RectTransform>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image Img(RectTransform rt, Color c, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = raycast;
            return img;
        }

        Text Txt(RectTransform rt, string s, int size, Color c, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.85f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        Text Label(Transform parent, string s, int size, Color c, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 boxSize,
            TextAnchor align, FontStyle style = FontStyle.Normal)
            => Txt(Rect("Text", parent, anchor, pivot, pos, boxSize), s, size, c, align, style);

        Button Btn(Transform parent, string label, Vector2 pos, Vector2 size, UnityAction onClick, int fontSize = 34)
        {
            var rt = Rect("Nut_" + label, parent, new(0.5f, 0.5f), new(0.5f, 0.5f), pos, size);
            var img = Img(rt, Color.white, true);
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = ButtonNormal;
            cb.highlightedColor = ButtonHover;
            cb.selectedColor = ButtonNormal;
            cb.pressedColor = new Color(0.3f, 0.1f, 0.05f, 1f);
            cb.colorMultiplier = 1f;
            b.colors = cb;
            b.onClick.AddListener(() =>
            {
                GM.Audio.Play(Sfx.Click, 0.6f, 0f);
                onClick();
            });
            Txt(Stretch("Chu", rt), label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return b;
        }

        GameObject Overlay(string name, Color bg)
        {
            var rt = Stretch(name, canvasRect);
            Img(rt, bg, true);
            return rt.gameObject;
        }

        RectTransform Bar(Transform parent, Vector2 pos, Vector2 size, Color fill, out Text label)
        {
            var bg = Rect("Thanh", parent, new(0f, 1f), new(0f, 1f), pos, size);
            Img(bg, new Color(0.12f, 0.05f, 0.04f, 0.9f));
            var f = Stretch("Fill", bg);
            Img(f, fill);
            label = Txt(Stretch("Chu", bg), "", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return f;
        }

        static void SetFill(RectTransform fill, float fraction)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            fill.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ Các màn hình
        void BuildHud()
        {
            hud = Stretch("HUD", canvasRect).gameObject;
            var t = hud.transform;

            var frame = Rect("KhungGiong", t, new(0f, 1f), new(0f, 1f), new(30f, -30f), new(470f, 125f));
            Img(frame, new Color(0f, 0f, 0f, 0.45f));
            Label(frame, "THÁNH GIÓNG", 24, Gold, new(0f, 1f), new(0f, 1f), new(20f, -8f), new(250f, 30f), TextAnchor.UpperLeft, FontStyle.Bold);
            growthText = Label(frame, "", 22, Color.white, new(1f, 1f), new(1f, 1f), new(-20f, -8f), new(200f, 30f), TextAnchor.UpperRight);
            hpFill = Bar(frame, new(20f, -44f), new(430f, 30f), new Color(0.85f, 0.15f, 0.1f), out hpText);
            energyFill = Bar(frame, new(20f, -84f), new(430f, 24f), new Color(1f, 0.55f, 0.08f), out energyText);

            waveText = Label(t, "", 36, Color.white, new(0.5f, 1f), new(0.5f, 1f), new(0f, -25f), new(900f, 50f), TextAnchor.UpperCenter, FontStyle.Bold);
            scoreText = Label(t, "", 28, Color.white, new(1f, 1f), new(1f, 1f), new(-30f, -30f), new(420f, 120f), TextAnchor.UpperRight);

            var bb = Rect("ThanhTuongGiac", t, new(0.5f, 1f), new(0.5f, 1f), new(0f, -85f), new(900f, 70f));
            bossBar = bb.gameObject;
            bossName = Label(bb, BossEnemy.BossName, 28, new Color(1f, 0.5f, 0.4f), new(0.5f, 1f), new(0.5f, 1f), Vector2.zero, new(900f, 34f), TextAnchor.UpperCenter, FontStyle.Bold);
            var bossBg = Rect("Nen", bb, new(0.5f, 1f), new(0.5f, 1f), new(0f, -36f), new(900f, 26f));
            Img(bossBg, new Color(0.12f, 0.05f, 0.04f, 0.9f));
            bossFill = Stretch("Fill", bossBg);
            Img(bossFill, new Color(0.75f, 0.1f, 0.08f));

            promptText = Label(t, "[E] Nhổ tre ngà", 34, Palette.FireYellow, new(0.5f, 0f), new(0.5f, 0f), new(0f, 160f), new(600f, 50f), TextAnchor.MiddleCenter, FontStyle.Bold);
            bambooText = Label(t, "", 30, Palette.BambooLight, new(0.5f, 0f), new(0.5f, 0f), new(0f, 210f), new(600f, 50f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(t, "WASD: Di chuyển   J/Chuột trái: Đánh   Space: Lướt   K/Chuột phải: Phun lửa   E: Nhổ tre   Esc: Tạm dừng",
                22, new Color(1f, 1f, 1f, 0.8f), new(0f, 0f), new(0f, 0f), new(30f, 20f), new(1500f, 36f), TextAnchor.LowerLeft);

            hurtOverlay = Img(Stretch("ManDo", canvasRect), new Color(0.8f, 0f, 0f, 0f));
        }

        void BuildBanner()
        {
            var rt = Rect("Banner", canvasRect, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 230f), new(1600f, 220f));
            bannerGroup = rt.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;
            bannerTitle = Label(rt, "", 72, Gold, new(0.5f, 1f), new(0.5f, 1f), Vector2.zero, new(1600f, 100f), TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerSub = Label(rt, "", 36, Color.white, new(0.5f, 1f), new(0.5f, 1f), new(0f, -105f), new(1600f, 60f), TextAnchor.MiddleCenter);
            bannerGroup.alpha = 0f;
        }

        void BuildMenu()
        {
            menu = Overlay("Menu", new Color(0f, 0f, 0f, 0.3f));
            var t = menu.transform;
            Label(t, "THÁNH GIÓNG", 140, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 270f), new(1600f, 170f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(t, "Truyền thuyết phá giặc Ân  ·  Game hành động 2.5D", 38, Color.white, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 160f), new(1600f, 60f), TextAnchor.MiddleCenter);
            Btn(t, "BẮT ĐẦU", new(0f, 20f), new(420f, 80f), () => GM.OnStartPressed());
            Btn(t, "HƯỚNG DẪN", new(0f, -80f), new(420f, 80f), ToggleHelp);
            Btn(t, "THOÁT", new(0f, -180f), new(420f, 80f), () => GM.QuitGame());
            menuHigh = Label(t, "", 32, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, -290f), new(1000f, 50f), TextAnchor.MiddleCenter);
            Label(t, "Enter: Bắt đầu   ·   H: Hướng dẫn", 26, new Color(1f, 1f, 1f, 0.75f), new(0.5f, 0f), new(0.5f, 0f), new(0f, 30f), new(1200f, 40f), TextAnchor.MiddleCenter);
        }

        void BuildStory()
        {
            story = Overlay("CotTruyen", new Color(0f, 0f, 0f, 0.78f));
            var t = story.transform;
            Label(t, "TRUYỀN THUYẾT THÁNH GIÓNG", 48, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 300f), new(1600f, 70f), TextAnchor.MiddleCenter, FontStyle.Bold);
            storyText = Label(t, "", 40, Color.white, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 30f), new(1500f, 400f), TextAnchor.MiddleCenter);
            storyText.lineSpacing = 1.3f;
            Label(t, "Enter / Space / Click: Tiếp tục    ·    Esc: Bỏ qua", 28, new Color(1f, 1f, 1f, 0.7f), new(0.5f, 0f), new(0.5f, 0f), new(0f, 60f), new(1400f, 40f), TextAnchor.MiddleCenter);
        }

        void BuildUpgrade()
        {
            upgrade = Overlay("NangCap", new Color(0f, 0f, 0f, 0.6f));
            var t = upgrade.transform;
            Label(t, "GIÓNG VƯƠN VAI LỚN THÊM!", 66, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 310f), new(1600f, 90f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(t, "Chọn một sức mạnh  (phím 1, 2, 3 hoặc click)", 34, Color.white, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 225f), new(1600f, 50f), TextAnchor.MiddleCenter);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = Btn(t, "", new((i - 1) * 440f, -30f), new(400f, 320f), () => GM.ChooseUpgrade(index));
                var card = (RectTransform)b.transform;
                Label(card, $"[{i + 1}]", 30, new Color(1f, 1f, 1f, 0.7f), new(0.5f, 1f), new(0.5f, 1f), new(0f, -20f), new(380f, 40f), TextAnchor.MiddleCenter);
                upgradeTitles.Add(Label(card, "", 38, Gold, new(0.5f, 1f), new(0.5f, 1f), new(0f, -75f), new(370f, 100f), TextAnchor.MiddleCenter, FontStyle.Bold));
                upgradeDescs.Add(Label(card, "", 30, Color.white, new(0.5f, 1f), new(0.5f, 1f), new(0f, -180f), new(360f, 120f), TextAnchor.UpperCenter));
            }
        }

        void BuildPause()
        {
            pause = Overlay("TamDung", PanelBg);
            var t = pause.transform;
            Label(t, "TẠM DỪNG", 90, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 260f), new(1200f, 120f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Btn(t, "TIẾP TỤC", new(0f, 90f), new(420f, 80f), () => GM.Resume());
            Btn(t, "CHƠI LẠI", new(0f, -10f), new(420f, 80f), () => GM.StartGame());
            Btn(t, "HƯỚNG DẪN", new(0f, -110f), new(420f, 80f), ToggleHelp);
            Btn(t, "VỀ MENU", new(0f, -210f), new(420f, 80f), () => GM.GoToMenu());
            Label(t, "Esc: Tiếp tục   ·   R: Chơi lại   ·   M: Về menu", 26, new Color(1f, 1f, 1f, 0.75f), new(0.5f, 0f), new(0.5f, 0f), new(0f, 40f), new(1200f, 40f), TextAnchor.MiddleCenter);
        }

        void BuildEnd()
        {
            endPanel = Overlay("KetThuc", PanelBg);
            var t = endPanel.transform;
            endTitle = Label(t, "", 90, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 280f), new(1700f, 120f), TextAnchor.MiddleCenter, FontStyle.Bold);
            endBody = Label(t, "", 36, Color.white, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 70f), new(1400f, 280f), TextAnchor.MiddleCenter);
            endBody.lineSpacing = 1.25f;
            Btn(t, "CHƠI LẠI", new(-230f, -170f), new(400f, 80f), () => GM.StartGame());
            Btn(t, "VỀ MENU", new(230f, -170f), new(400f, 80f), () => GM.GoToMenu());
            Label(t, "Enter / R: Chơi lại   ·   M / Esc: Về menu", 26, new Color(1f, 1f, 1f, 0.75f), new(0.5f, 0f), new(0.5f, 0f), new(0f, 40f), new(1200f, 40f), TextAnchor.MiddleCenter);
        }

        void BuildHelp()
        {
            help = Overlay("HuongDan", new Color(0f, 0f, 0f, 0.88f));
            var t = help.transform;
            Label(t, "HƯỚNG DẪN", 64, Gold, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 400f), new(1200f, 80f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var body = Label(t, HelpText, 30, Color.white, new(0.5f, 0.5f), new(0.5f, 0.5f), new(0f, 20f), new(1300f, 640f), TextAnchor.MiddleLeft);
            body.lineSpacing = 1.15f;
            Btn(t, "ĐÓNG (H)", new(0f, -400f), new(360f, 76f), ToggleHelp);
        }

        // ------------------------------------------------------------------ API cho GameManager
        public void HideAll()
        {
            hud.SetActive(false);
            menu.SetActive(false);
            story.SetActive(false);
            upgrade.SetActive(false);
            pause.SetActive(false);
            endPanel.SetActive(false);
            help.SetActive(false);
            EndPanelShown = false;
            bannerTimer = 0f;
            bannerGroup.alpha = 0f;
            hurtAlpha = 0f;
            ClearFloating();
        }

        public void ShowMenu(int highScore)
        {
            HideAll();
            menu.SetActive(true);
            menuHigh.text = highScore > 0 ? $"Kỷ lục: {highScore:N0} điểm" : "";
        }

        public void ShowStory()
        {
            HideAll();
            story.SetActive(true);
            storyIndex = 0;
            storyText.text = StorySlides[0];
        }

        /// <summary>Chuyển trang cốt truyện; trả về false khi đã hết.</summary>
        public bool NextStorySlide()
        {
            storyIndex++;
            if (storyIndex >= StorySlides.Length) return false;
            storyText.text = StorySlides[storyIndex];
            GM.Audio.Play(Sfx.Click, 0.5f, 0f);
            return true;
        }

        public void ShowHud()
        {
            HideAll();
            hud.SetActive(true);
        }

        public void ShowPause(bool show)
        {
            pause.SetActive(show);
            if (!show) help.SetActive(false);
        }

        public void ToggleHelp() => help.SetActive(!help.activeSelf);

        public void ShowUpgrades(List<Upgrade> options)
        {
            upgrade.SetActive(true);
            bannerTimer = 0f; // ẩn banner để không đè lên tiêu đề
            for (int i = 0; i < upgradeTitles.Count; i++)
            {
                bool has = i < options.Count;
                upgradeTitles[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;
                upgradeTitles[i].text = options[i].Name;
                upgradeDescs[i].text = options[i].Description;
            }
        }

        public void HideUpgrades() => upgrade.SetActive(false);

        public void ShowGameOver(int score, int kills, int wave, int high, bool newRecord)
        {
            ShowEnd("GIÓNG ĐÃ NGÃ XUỐNG...", new Color(1f, 0.45f, 0.35f),
                $"Gióng đã chiến đấu đến hơi thở cuối cùng ở đợt {wave}.\n\n" +
                $"Điểm: <b>{score:N0}</b>      Hạ gục: <b>{kills}</b> giặc\n" +
                (newRecord ? "<color=#FFD15A>KỶ LỤC MỚI!</color>" : $"Kỷ lục: {high:N0}"));
        }

        public void ShowVictory(int score, int kills, float time, int high, bool newRecord)
        {
            int m = (int)(time / 60f), s = (int)(time % 60f);
            ShowEnd("ĐÁNH TAN GIẶC ÂN!", Gold,
                "Giặc Ân tan vỡ. Gióng cưỡi ngựa sắt lên đỉnh Sóc Sơn rồi bay thẳng về trời.\nVua phong là <b>Phù Đổng Thiên Vương</b>.\n\n" +
                $"Điểm: <b>{score:N0}</b>      Hạ gục: <b>{kills}</b> giặc      Thời gian: <b>{m:00}:{s:00}</b>\n" +
                (newRecord ? "<color=#FFD15A>KỶ LỤC MỚI!</color>" : $"Kỷ lục: {high:N0}"));
        }

        void ShowEnd(string title, Color titleColor, string body)
        {
            hud.SetActive(false);
            endPanel.SetActive(true);
            endTitle.text = title;
            endTitle.color = titleColor;
            endBody.text = body;
            EndPanelShown = true;
        }

        public void ShowBanner(string title, string subtitle, float duration)
        {
            bannerTitle.text = title;
            bannerSub.text = subtitle;
            bannerDuration = bannerTimer = duration;
        }

        public void FlashHurt() => hurtAlpha = 0.35f;

        public void FloatText(Vector3 world, string text, Color color, float scale = 1f)
        {
            FloatingText f;
            if (floatPool.Count > 0) f = floatPool.Pop();
            else
            {
                var rt = Rect("Chu", floatRoot, new(0.5f, 0.5f), new(0.5f, 0.5f), Vector2.zero, new(500f, 80f));
                f = new FloatingText { rt = rt, text = Txt(rt, "", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold) };
            }
            f.rt.gameObject.SetActive(true);
            f.text.text = text;
            f.text.color = color;
            f.text.fontSize = Mathf.RoundToInt(34 * scale);
            f.world = world + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            f.life = f.maxLife = 0.9f;
            floats.Add(f);
            PositionFloat(f);
        }

        public void ClearFloating()
        {
            foreach (var f in floats)
            {
                f.rt.gameObject.SetActive(false);
                floatPool.Push(f);
            }
            floats.Clear();
        }

        void PositionFloat(FloatingText f)
        {
            var cam = GM != null && GM.Cam != null ? GM.Cam.Cam : Camera.main;
            if (cam == null) return;
            float k = 1f - f.life / f.maxLife;
            Vector3 sp = cam.WorldToScreenPoint(f.world + Vector3.up * k * 1.5f);
            if (sp.z < 0f) { f.rt.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatRoot, sp, null, out Vector2 local);
            f.rt.anchoredPosition = local;
            var c = f.text.color;
            c.a = Mathf.Clamp01(f.life / (f.maxLife * 0.4f));
            f.text.color = c;
        }

        // ------------------------------------------------------------------ Cập nhật mỗi khung hình
        void Update()
        {
            float udt = Time.unscaledDeltaTime;

            for (int i = floats.Count - 1; i >= 0; i--)
            {
                var f = floats[i];
                f.life -= udt;
                if (f.life <= 0f)
                {
                    f.rt.gameObject.SetActive(false);
                    floatPool.Push(f);
                    floats.RemoveAt(i);
                    continue;
                }
                PositionFloat(f);
            }

            if (bannerTimer > 0f)
            {
                bannerTimer -= udt;
                float elapsed = bannerDuration - bannerTimer;
                bannerGroup.alpha = Mathf.Min(Mathf.Clamp01(elapsed / 0.3f), Mathf.Clamp01(bannerTimer / 0.6f));
            }
            else bannerGroup.alpha = 0f;

            hurtAlpha = Mathf.Max(0f, hurtAlpha - udt * 1.2f);
            hurtOverlay.color = new Color(0.8f, 0f, 0f, hurtAlpha);

            if (hud.activeSelf && GM != null) UpdateHud();
        }

        void UpdateHud()
        {
            var p = GM.Player;
            SetFill(hpFill, p.HP / p.MaxHP);
            hpText.text = $"Máu  {Mathf.CeilToInt(p.HP)} / {Mathf.CeilToInt(p.MaxHP)}";
            SetFill(energyFill, p.Energy / Player.MaxEnergy);
            energyText.text = p.Energy >= p.FireCost ? $"Lửa  {Mathf.FloorToInt(p.Energy)}  —  Sẵn sàng (K)" : $"Lửa  {Mathf.FloorToInt(p.Energy)}";
            growthText.text = $"Tầm vóc: cấp {p.GrowthLevel}";

            var w = GM.Waves;
            waveText.text = $"ĐỢT {Mathf.Max(1, w.CurrentWave)}/{WaveManager.TotalWaves}   ·   Giặc còn: {w.Remaining}";
            int m = (int)(GM.PlayTime / 60f), s = (int)(GM.PlayTime % 60f);
            scoreText.text = $"Điểm: {GM.Score:N0}\nHạ gục: {GM.Kills}\nThời gian: {m:00}:{s:00}";

            promptText.gameObject.SetActive(p.NearbyBamboo != null && !p.HasBamboo && GM.State == GameState.Playing);
            bambooText.text = p.HasBamboo ? $"Tre ngà: {Mathf.CeilToInt(p.BambooTimeLeft)}s" : "";

            var boss = w.Boss;
            bool showBoss = boss != null && !boss.IsDead;
            bossBar.SetActive(showBoss);
            if (showBoss)
            {
                SetFill(bossFill, boss.HP / boss.MaxHP);
                bossName.text = boss.Phase2 ? BossEnemy.BossName + " — NỔI GIẬN" : BossEnemy.BossName;
            }
        }
    }
}
