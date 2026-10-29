using System;
using UnityEngine;
using ZeroGCargo.Core;
using ZeroGCargo.Data;
using ZeroGCargo.UI;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Prewarmed original 3D models and reusable line renderers on the XZ station plane.</summary>
    public sealed class StationView
    {
        private readonly GameObject root;
        private readonly Transform[] shuttles = new Transform[4];
        private readonly Transform[] bays = new Transform[4];
        private readonly Transform[] hazards = new Transform[16];
        private readonly LineRenderer[] routes = new LineRenderer[4];
        private readonly LineRenderer[] hints = new LineRenderer[4];
        private readonly GameObject[,] shuttleVariants = new GameObject[4,4];
        private readonly GameObject[,] hazardVariants = new GameObject[16,3];
        private readonly Material[] materials = new Material[12];
        private int materialCount;
        private readonly GameConfig config;
        private readonly Material lineMaterial;
        private readonly Transform[] sparks = new Transform[24];
        private readonly Float2[] sparkCenters = new Float2[24];
        private readonly float[] sparkAge = new float[24];
        private int sparkCursor;
        /// <summary>Creates all reusable models and effects before gameplay.</summary>
        public StationView(GameConfig config)
        {
            this.config = config; root = new GameObject("3D station and prewarmed models"); UnityEngine.Object.DontDestroyOnLoad(root);
            lineMaterial = new Material(Resources.Load<Material>("Config/Station3DLineMaterial")); lineMaterial.color = ColorTokens.Accent;
            CreateModel("StationDeck", root.transform);
            for (int i = 0; i < 4; i++)
            {
                shuttles[i] = new GameObject("Shuttle " + i).transform; shuttles[i].SetParent(root.transform);
                for (int c = 0; c < 4; c++) { shuttleVariants[i,c] = CreateModel("Shuttle" + ((CargoColor)c), shuttles[i]); shuttleVariants[i,c].SetActive(false); }
                bays[i] = CreateModel("Dock", root.transform).transform;
                routes[i] = Line("Flight route", config.LineWidth); hints[i] = Line("Guide route", config.LineWidth);
                hints[i].startColor = new Color(0,1,1,.25f); hints[i].endColor = new Color(0,1,1,.4f);
            }
            string[] names = { "Meteorite", "EnergyBarrier", "GravityWell" };
            for (int i = 0; i < 16; i++)
            {
                hazards[i] = new GameObject("Hazard " + i).transform; hazards[i].SetParent(root.transform);
                for (int k = 0; k < 3; k++) { hazardVariants[i,k] = CreateModel(names[k], hazards[i]); hazardVariants[i,k].SetActive(false); }
            }
            // Prewarm dock spark geometry once; never instantiate effects in flight.
            for (int i = 0; i < sparks.Length; i++)
            {
                var obj = new GameObject("Dock sparkle"); obj.transform.SetParent(root.transform);
                var filter = obj.AddComponent<MeshFilter>(); filter.sharedMesh = SparkMesh();
                var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = lineMaterial;
                sparks[i] = obj.transform; obj.SetActive(false); sparkAge[i] = 1;
            }
        }
        private static Mesh sparkMesh;
        private static Mesh SparkMesh()
        {
            if (sparkMesh != null) return sparkMesh;
            sparkMesh = new Mesh { name = "Shared dock sparkle" };
            sparkMesh.vertices = new[] { new Vector3(-.02f,0,0),new Vector3(0,.02f,0),new Vector3(.02f,0,0),new Vector3(0,-.02f,0) };
            sparkMesh.triangles = new[] {0,1,2,0,2,3}; sparkMesh.RecalculateNormals(); return sparkMesh;
        }
        private GameObject CreateModel(string name, Transform parent)
        {
            var asset = Resources.Load<GameObject>("Models/" + name);
            if (asset == null) throw new InvalidOperationException("Missing 3D model: " + name);
            var obj = UnityEngine.Object.Instantiate(asset, parent); obj.name = name;
            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
            {
                Material[] source = renderer.sharedMaterials;
                for (int i = 0; i < source.Length; i++) source[i] = Surface(source[i] == null ? "Hull" : source[i].name);
                renderer.sharedMaterials = source;
            }
            return obj;
        }
        private Material Surface(string name)
        {
            name = name.Replace(" (Instance)", "");
            for (int i = 0; i < materialCount; i++) if (materials[i].name == name) return materials[i];
            var mat = new Material(Resources.Load<Material>("Config/StationSurfaceMaterial")) { name = name };
            Color color = new Color(.09f,.13f,.20f); bool emissive = false;
            if (name.Contains("Steel")) color = new Color(.22f,.30f,.40f);
            if (name.Contains("Deck")) color = new Color(.025f,.04f,.07f);
            if (name.Contains("Rock")) color = new Color(.17f,.21f,.28f);
            if (name.Contains("Glass")) color = new Color(.02f,.23f,.30f);
            if (name.Contains("Cyan")) { color = ColorTokens.Accent; emissive = true; }
            if (name.Contains("Danger")) { color = new Color32(255,68,68,255); emissive = true; }
            for (int c = 0; c < 4; c++) if (name.Contains(((CargoColor)c).ToString())) { color = ColorTokens.Cargo((CargoColor)c); emissive = true; }
            mat.color = color; mat.SetFloat("_Metallic", emissive ? .15f : .5f); mat.SetFloat("_Smoothness", .4f);
            if (emissive) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * .6f); }
            if (materialCount >= materials.Length) throw new InvalidOperationException("Material palette exceeded");
            materials[materialCount++] = mat; return mat;
        }
        private LineRenderer Line(string name, float width)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root.transform);
            var line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial; line.widthMultiplier = width;
            line.startColor = new Color(0,1,1,.15f); line.endColor = ColorTokens.Accent; line.numCornerVertices = 4; line.numCapVertices = 4;
            line.positionCount = 0; return line;
        }
        private static Vector3 World(Float2 p, float height = .1f) => new Vector3(p.X, height, p.Y);
        /// <summary>Places the reusable objects for a new validated level.</summary>
        public void Load(LevelDefinition data)
        {
            root.SetActive(true);
            for (int i = 0; i < 4; i++)
            {
                shuttles[i].gameObject.SetActive(i < data.cargo.Length); bays[i].gameObject.SetActive(i < data.bays.Length);
                for (int c = 0; c < 4; c++) shuttleVariants[i,c].SetActive(i < data.cargo.Length && c == data.cargo[i].color);
                routes[i].positionCount = 0; hints[i].positionCount = 0;
                if (i >= data.cargo.Length) continue;
                shuttles[i].position = World(data.cargo[i].spawn.Vector, 0); shuttles[i].localScale = Vector3.one;
                bays[i].position = World(data.bays[i].position.Vector, -.015f);
                foreach (var renderer in bays[i].GetComponentsInChildren<MeshRenderer>())
                    if (renderer.name.Contains("Cargo identification")) renderer.sharedMaterial = Surface(((CargoColor)data.bays[i].color).ToString());
            }
            for (int i = 0; i < 16; i++)
            {
                hazards[i].gameObject.SetActive(i < data.hazards.Length);
                for (int k = 0; k < 3; k++) hazardVariants[i,k].SetActive(i < data.hazards.Length && data.hazards[i].kind == k);
                if (i >= data.hazards.Length) continue;
                var h = data.hazards[i]; hazards[i].position = World(h.position.Vector, 0);
                hazards[i].localScale = h.kind == 0 ? Vector3.one * (h.radius / .44f) : h.kind == 1 ? new Vector3(h.halfWidth * 2, 1, h.halfHeight * 2 / .085f) : new Vector3(h.radius / .68f, 1, h.radius / .68f);
            }
            for (int i = 0; i < sparks.Length; i++) { sparks[i].gameObject.SetActive(false); sparkAge[i] = 1; }
        }
        /// <summary>Updates only transforms, line points and pooled particle state.</summary>
        public void Render(CampaignSimulation simulation, PathData input, int selected, float dt)
        {
            for (int i = 0; i < simulation.Level.cargo.Length; i++)
            {
                var s = simulation.Shuttle(i); shuttles[i].position = World(s.Position, .07f);
                shuttles[i].rotation = Quaternion.Euler(0,-s.Angle * Mathf.Rad2Deg,0);
                float scale = s.Phase == 4 ? 0 : s.Phase == 3 ? 1 - Mathf.Clamp01(s.Animation / .3f) : 1;
                shuttles[i].localScale = Vector3.one * (scale*.5f);
                routes[i].startColor = new Color(0,1,1,s.Route.Length > 0 ? Mathf.Lerp(.4f,0,Mathf.Clamp01(s.Flight.Progress/s.Route.Length)) : .4f);
                Draw(routes[i], selected == i && s.CanDraw && input.Count > 0 ? input : s.Route);
                bool show = simulation.HintVisible && simulation.Level.id <= 10 && s.Phase == 0;
                var points = simulation.Level.solutions[i].points; hints[i].positionCount = show ? points.Length : 0;
                if (show) for (int k = 0; k < points.Length; k++) hints[i].SetPosition(k, World(points[k].Vector,.11f));
                bays[i].localScale = Vector3.one * (simulation.BayUnlocked(i) ? 1 : .65f);
            }
            for (int i = 0; i < simulation.Level.hazards.Length; i++)
            {
                var h = simulation.Level.hazards[i]; hazards[i].position = World(h.At(simulation.Elapsed),0);
                if (h.kind == 2) hazards[i].rotation = Quaternion.Euler(0,simulation.Elapsed*12,0);
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparkAge[i] >= .6f) continue;
                sparkAge[i] += dt; float angle = i * 2.4f, distance = sparkAge[i] * 1.2f;
                sparks[i].position = World(sparkCenters[i] + new Float2(Mathf.Cos(angle),Mathf.Sin(angle))*distance,.3f);
                sparks[i].localScale = Vector3.one * (1 - sparkAge[i] / .6f); sparks[i].rotation = Quaternion.Euler(90,0,angle*Mathf.Rad2Deg);
                if (sparkAge[i] >= .6f) sparks[i].gameObject.SetActive(false);
            }
        }
        private static void Draw(LineRenderer line, PathData path)
        { line.positionCount = path.Count; for (int i = 0; i < path.Count; i++) line.SetPosition(i,World(path[i],.10f)); }
        /// <summary>Emits a bounded prewarmed sparkle burst.</summary>
        public void Spark(Float2 position)
        { for (int i = 0; i < 8; i++) { int at = sparkCursor++ % sparks.Length; sparkCenters[at] = position; sparkAge[at] = 0; sparks[at].gameObject.SetActive(true); } }
        /// <summary>Shows or hides the station.</summary>
        public void SetVisible(bool visible) => root.SetActive(visible);
        /// <summary>Releases owned runtime materials and geometry.</summary>
        public void Dispose()
        {
            for (int i = 0; i < materialCount; i++) UnityEngine.Object.Destroy(materials[i]);
            UnityEngine.Object.Destroy(lineMaterial); UnityEngine.Object.Destroy(root);
            if (sparkMesh != null) { UnityEngine.Object.Destroy(sparkMesh); sparkMesh = null; }
        }
    }
}
