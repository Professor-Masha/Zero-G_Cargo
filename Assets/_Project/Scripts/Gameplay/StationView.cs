using UnityEngine;
using ZeroGCargo.Data;
using ZeroGCargo.UI;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Reusable placeholder art and route buffers; allocated only at scene startup.</summary>
    public sealed class StationView
    {
        private readonly GameObject root;
        private readonly Transform shuttle;
        private readonly Transform bay;
        private readonly Transform meteorite;
        private readonly SpriteRenderer shuttleSprite;
        private readonly LineRenderer route;
        private readonly LineRenderer hint;
        private readonly LineRenderer bayLine;
        private readonly Vector3[] buffer;
        private readonly Material material;
        private readonly Sprite triangleSprite;
        private readonly Sprite circleSprite;
        /// <summary>Creates one reusable station; no instantiate/destroy occurs in flight.</summary>
        public StationView(GameConfig config)
        {
            root = new GameObject("Station (reused between levels)");
            Object.DontDestroyOnLoad(root);
            Material template = Resources.Load<Material>("Config/StationLineMaterial");
            if (template == null) throw new System.InvalidOperationException("Run Zero-G Cargo > Prepare prototype to create the station material.");
            material = new Material(template);
            triangleSprite = CreateSprite(true); circleSprite = CreateSprite(false);
            shuttleSprite = MakeSprite("Shuttle", triangleSprite, ColorTokens.Cargo(CargoColor.Magenta));
            shuttle = shuttleSprite.transform; shuttle.localScale = Vector3.one * 0.42f;
            bayLine = MakeLine("Docking bay", 0.035f, ColorTokens.Accent);
            bayLine.loop = true; bayLine.positionCount = 6; bay = bayLine.transform;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3;
                bayLine.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * config.DockRadius);
            }
            bayLine.useWorldSpace = false;
            var core = MakeSprite("Cargo marker", circleSprite, ColorTokens.Cargo(CargoColor.Magenta));
            core.transform.SetParent(bay, false); core.transform.localScale = Vector3.one * 0.18f;
            meteorite = MakeSprite("Meteorite", circleSprite, new Color32(90, 102, 123, 255)).transform;
            route = MakeLine("Comet route", config.LineWidth, ColorTokens.Accent);
            route.startColor = new Color(0, 0.94f, 1, 0.25f); route.endColor = ColorTokens.Accent;
            hint = MakeLine("Ghost hint", config.LineWidth, new Color(0, 0.94f, 1, 0.3f));
            buffer = new Vector3[config.PathCapacity];
        }
        private SpriteRenderer MakeSprite(string name, Sprite sprite, Color color)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root.transform);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sharedMaterial = material; return renderer;
        }
        private LineRenderer MakeLine(string name, float width, Color color)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root.transform);
            var line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.widthMultiplier = width;
            line.startColor = color; line.endColor = color; line.numCornerVertices = 3; line.numCapVertices = 3;
            line.positionCount = 0; line.sortingOrder = -1; return line;
        }
        private static Sprite CreateSprite(bool triangle)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2 - 1, ny = (y + 0.5f) / size * 2 - 1;
                bool inside = triangle ? nx >= -0.8f && nx <= 0.95f && Mathf.Abs(ny) <= (0.95f - nx) * 0.52f : nx * nx + ny * ny <= 0.94f;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels); texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        /// <summary>Resets the station for an authored level.</summary>
        public void Load(LevelData level)
        {
            root.SetActive(true); shuttle.position = level.Spawn; shuttle.rotation = Quaternion.identity;
            shuttleSprite.color = ColorTokens.Cargo(level.Cargo); shuttle.localScale = Vector3.one * 0.42f;
            bay.position = level.Bay; bay.GetComponentInChildren<SpriteRenderer>().color = ColorTokens.Cargo(level.BayCargo);
            meteorite.position = level.Meteorite; meteorite.localScale = Vector3.one * (level.MeteoriteRadius * 2);
            route.positionCount = 0; hint.positionCount = 0;
        }
        /// <summary>Shows or hides the board without destroying pooled objects.</summary>
        public void SetVisible(bool visible) => root.SetActive(visible);
        /// <summary>Updates shuttle transform.</summary>
        public void Move(Vector2 position, float angle, float scale = 1)
        { shuttle.position = position; shuttle.rotation = Quaternion.Euler(0, 0, angle); shuttle.localScale = Vector3.one * (0.42f * scale); }
        /// <summary>Renders the route, fading the already-travelled portion.</summary>
        public void DrawPath(Core.PathData path, float progress)
        {
            route.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                Core.Float2 p = path[i]; buffer[i] = new Vector3(p.X, p.Y, 0);
                route.SetPosition(i, buffer[i]);
            }
            route.startColor = new Color(0, 0.94f, 1, path.Length > 0 ? Mathf.Lerp(0.25f, 0, progress / path.Length) : 0.25f);
        }
        /// <summary>Shows a tutorial solution guide.</summary>
        public void ShowHint(Vector2[] points, bool visible)
        {
            hint.positionCount = visible ? points.Length : 0;
            if (visible) for (int i = 0; i < points.Length; i++) hint.SetPosition(i, points[i]);
        }
        /// <summary>Releases owned generated resources when the scene is destroyed.</summary>
        public void Dispose()
        {
            Object.Destroy(material); Object.Destroy(triangleSprite.texture); Object.Destroy(circleSprite.texture);
            Object.Destroy(triangleSprite); Object.Destroy(circleSprite); Object.Destroy(root);
        }
    }
}
