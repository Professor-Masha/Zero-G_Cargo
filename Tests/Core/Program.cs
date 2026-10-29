using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.Json;
using ZeroGCargo.Core;

var tests = new List<(string Name, Action Run)>();
void Test(string name, Action action) => tests.Add((name, action));
void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
void Near(float actual, float expected, float tolerance = 0.0001f) => Check(Math.Abs(actual - expected) <= tolerance, $"Expected {expected} ± {tolerance}, got {actual}");
var cfg = new FlightSettings(8, 6, 0.4f, 12, 0.15f, 0.3f);
PathData Route(params Float2[] points) { var p = new PathData(256); foreach (var v in points) p.Add(v, 6, 0); return p; }
var options = new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true };
SaveData Decode(string text)
{
    try { return JsonSerializer.Deserialize<SaveData>(text, options); }
    catch (JsonException e) { throw new FormatException("Invalid JSON", e); }
}
SaveStore Store(string path) => new SaveStore(path, Decode, d => JsonSerializer.Serialize(d, options));
void WithStore(Action<string, SaveStore> action)
{
    string dir = Path.Combine(Path.GetTempPath(), "zero-g-test-" + Guid.NewGuid()); Directory.CreateDirectory(dir);
    try { action(dir, Store(dir)); } finally { Directory.Delete(dir, true); }
}
Test("route clamps an oversized final segment", () => { var p = Route(new Float2(0, 0), new Float2(20, 0)); Near(p.Length, 6); Near(p.EndPoint.X, 6); });
Test("route ignores duplicate samples", () => { var p = Route(default, default, new Float2(1, 0)); Check(p.Count == 2); Near(p.Length, 1); });
Test("route interpolates by arc length", () => { var p = Route(default, new Float2(1, 0), new Float2(1, 2)); var v = p.GetPointAtDistance(2); Near(v.X, 1); Near(v.Y, 1); });
Test("route rejects NaN and infinity", () => { var p = new PathData(4); Check(!p.Add(new Float2(float.NaN, 0), 6, 0)); Check(!p.Add(new Float2(0, float.PositiveInfinity), 6, 0)); });
Test("route capacity cannot overflow", () => { var p = new PathData(2); p.Add(default, 6, 0); p.Add(new Float2(1, 0), 6, 0); Check(!p.Add(new Float2(2, 0), 6, 0)); });
Test("route can be cleared and reused", () => { var p = Route(default, new Float2(1, 0)); p.Clear(); Check(p.Count == 0); Near(p.Length, 0); p.Add(new Float2(2, 2), 6, 0); Near(p.EndPoint.X, 2); });
Test("route progress never moves backwards", () => { var p = Route(default, new Float2(4, 0)); Near(p.ProjectDistance(default, 2, 1), 2); });
Test("route projection cannot jump far ahead", () => { var p = Route(default, new Float2(4, 0)); Near(p.ProjectDistance(new Float2(4, 0), 0, 0.9f), 0); });
Test("one step matches acceleration and drag formula", () => { var f = new InertiaPhysics(); f.Reset(default); f.Follow(Route(default, new Float2(5, 0)), cfg); f.Step(1f / 60, default); Near(f.Velocity.X, 8f / 60 * (1 - 0.4f / 60)); });
Test("velocity is capped with external forces", () => { var f = new InertiaPhysics(); f.Reset(default); f.Follow(Route(default, new Float2(5, 0)), cfg); f.Step(1f / 60, new Float2(10000, 10000)); Near(f.Velocity.Magnitude, 6); });
Test("invalid dt rejected without poisoning state", () => { var f = new InertiaPhysics(); bool rejected = false; try { f.Step(float.NaN, default); } catch (ArgumentOutOfRangeException) { rejected = true; } Check(rejected); Check(f.Position.IsFinite); });
Test("straight flight brakes to rest with endpoint drift", () => {
    var f = new InertiaPhysics(); f.Reset(default); f.Follow(Route(default, new Float2(4, 0)), cfg);
    for (int i = 0; i < 1800 && f.IsMoving; i++) f.Step(1f / 60, default);
    Check(!f.IsMoving, "Never stopped"); Check(f.Position.X > 4 && f.Position.X <= 4 + cfg.Drift * cfg.MaxSpeed + cfg.MaxSpeed / 60, $"Expected overshoot, got {f.Position.X}");
});
Test("sharp turns retain momentum instead of snapping to route", () => {
    var f = new InertiaPhysics(); f.Reset(default); f.Follow(Route(default, new Float2(2, 0), new Float2(2, 2)), cfg);
    float widest = 0; for (int i = 0; i < 600 && f.IsMoving; i++) { f.Step(1f / 60, default); widest = Math.Max(widest, f.Position.X); }
    Check(widest > 2.05f, $"No wide swing: {widest}");
});
Float2 AtFrameRate(int fps)
{
    var f = new InertiaPhysics(); f.Reset(default); f.Follow(Route(default, new Float2(2, 0), new Float2(2, 2)), cfg);
    // Integer scheduling models Unity's fixed update count across a three-second render loop.
    int executed = 0;
    for (int frame = 1; frame <= fps * 3; frame++) { int due = frame * 60 / fps; while (executed < due) { f.Step(1f / 60, default); executed++; } }
    return f.Position;
}
Test("fixed simulation identical at 30, 60 and 120 render FPS", () => { var a = AtFrameRate(30); var b = AtFrameRate(60); var c = AtFrameRate(120); Near(a.X, b.X, 0); Near(b.Y, c.Y, 0); });
Test("reset clears momentum and flight state", () => { var f = new InertiaPhysics(); f.Reset(default); f.Follow(Route(default, new Float2(3, 0)), cfg); f.Step(1f / 60, default); f.Reset(new Float2(2, 1)); Check(!f.IsMoving); Near(f.Velocity.Magnitude, 0); Near(f.Position.X, 2); });
Test("swept collision catches tunnelling", () => Check(CollisionMath.SegmentCircle(new Float2(-2, 0), new Float2(2, 0), default, 0.5f)));
Test("swept collision rejects clear near miss", () => Check(!CollisionMath.SegmentCircle(new Float2(-2, 1), new Float2(2, 1), default, 0.5f)));
Test("zero-length sweep handles overlap", () => Check(CollisionMath.SegmentCircle(default, default, default, 0.5f)));
Test("save validation clamps and pads schema", () => { var s = new SaveData { highestUnlockedLevel = 70, totalScore = -8, starsPerLevel = new[] { 9, -2 }, adFrequencyCounter = 20 }; Check(s.Validate()); Check(s.highestUnlockedLevel == 50 && s.starsPerLevel.Length == 50 && s.starsPerLevel[0] == 3 && s.starsPerLevel[1] == 0 && s.totalScore == 0 && s.adFrequencyCounter == 4); });
Test("completion preserves best stars and unlocks next", () => { var s = new SaveData(); s.Complete(1, 3, 1); s.Complete(1, 1, 1); Check(s.highestUnlockedLevel == 2 && s.starsPerLevel[0] == 3 && s.totalScore == 2); });
Test("completion never unlocks level 51", () => { var s = new SaveData(); s.Complete(50, 3, 1); Check(s.highestUnlockedLevel == 50); });
Test("missing save creates defaults", () => WithStore((dir, store) => { var s = store.Load(); Check(s.highestUnlockedLevel == 1 && s.settings.soundOn && !store.WasReset); }));
Test("atomic save reloads progress and preferences", () => WithStore((dir, store) => { var s = store.Load(); s.Complete(1, 3, 1); s.settings.soundOn = false; Check(store.Write(s)); var loaded = Store(dir).Load(); Check(loaded.highestUnlockedLevel == 2 && !loaded.settings.soundOn && loaded.starsPerLevel[0] == 3); Check(!File.Exists(Path.Combine(dir, "zerog_save.json.tmp"))); }));
Test("second write preserves previous save as backup", () => WithStore((dir, store) => { var s = store.Load(); store.Write(s); s.Complete(1, 3, 1); store.Write(s); var backup = Decode(File.ReadAllText(Path.Combine(dir, "zerog_save.bak.json"))); Check(backup.highestUnlockedLevel == 1); }));
Test("corrupted primary recovers from backup", () => WithStore((dir, store) => { var s = store.Load(); s.Complete(1, 3, 1); store.Write(s); s.Complete(2, 2, 1); store.Write(s); File.WriteAllText(Path.Combine(dir, "zerog_save.json"), "broken"); var recovered = Store(dir).Load(); Check(recovered.highestUnlockedLevel == 2); }));
Test("recovery write preserves known-good backup", () => WithStore((dir, store) => { var s = store.Load(); store.Write(s); store.Write(s); File.WriteAllText(Path.Combine(dir, "zerog_save.json"), "broken"); var recoveredStore = Store(dir); var recovered = recoveredStore.Load(); recoveredStore.Write(recovered); Check(Decode(File.ReadAllText(Path.Combine(dir, "zerog_save.bak.json"))).Validate()); }));
Test("two corrupted saves reset gracefully", () => WithStore((dir, store) => { File.WriteAllText(Path.Combine(dir, "zerog_save.json"), "broken"); File.WriteAllText(Path.Combine(dir, "zerog_save.bak.json"), "broken"); Check(store.Load().highestUnlockedLevel == 1 && store.WasReset); }));
Test("future schema is never overwritten", () => WithStore((dir, store) => { string future = "{\"version\":2,\"totalScore\":99}"; File.WriteAllText(Path.Combine(dir, "zerog_save.json"), future); var s = store.Load(); Check(store.IsReadOnly && !store.Write(s)); Check(File.ReadAllText(Path.Combine(dir, "zerog_save.json")) == future); }));
Test("missing JSON fields retain defaults", () => { var s = Decode("{}"); Check(s.Validate() && s.settings.soundOn && s.highestUnlockedLevel == 1); });
using var tutorials = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tutorials.json")));
foreach (var raw in tutorials.RootElement.GetProperty("levels").EnumerateArray())
{
    var data = raw.Clone(); int id = data.GetProperty("id").GetInt32();
    Test($"tutorial {id} authored route is solvable and collision-free", () => {
        Float2 Read(JsonElement p) => new Float2(p.GetProperty("x").GetSingle(), p.GetProperty("y").GetSingle());
        Float2 spawn = Read(data.GetProperty("spawn")), bay = Read(data.GetProperty("bay")), meteor = Read(data.GetProperty("meteorite"));
        float radius = data.GetProperty("radius").GetSingle() + 0.16f;
        var path = new PathData(256);
        foreach (var p in data.GetProperty("route").EnumerateArray()) path.Add(Read(p), 6, 0);
        Check((path.EndPoint - bay).Magnitude < 0.001f, "Guide exceeds path budget");
        var f = new InertiaPhysics(); f.Reset(spawn); f.Follow(path, cfg, true); bool docked = false;
        for (int i = 0; i < 1800; i++)
        {
            var previous = f.Position; f.Step(1f / 60, default);
            Check(!CollisionMath.SegmentCircle(previous, f.Position, meteor, radius), $"Route collided at {f.Position.X}, {f.Position.Y}");
            Check(Math.Abs(f.Position.X) <= 3 && Math.Abs(f.Position.Y) <= 3.5f, "Route left station");
            if ((f.Position - bay).Magnitude <= 0.48f && f.Velocity.Magnitude <= 1.8f) { docked = true; break; }
            if (!f.IsMoving) break;
        }
        Check(docked, $"Guide did not dock: ended {f.Position.X}, {f.Position.Y}, distance {(f.Position - bay).Magnitude}");
    });
}
Test("matching cargo requires safe docking speed", () => { Check(DockingRules.Evaluate(default, default, 0.48f, 1, 1.8f, 0, 0) == 1); Check(DockingRules.Evaluate(default, default, 0.48f, 3, 1.8f, 0, 0) == 0); });
Test("wrong-colour bay rejects cargo", () => Check(DockingRules.Evaluate(default, default, 0.48f, 0, 1.8f, 1, 0) == -1));
Test("distant bay does not capture cargo", () => Check(DockingRules.Evaluate(new Float2(3, 0), default, 0.48f, 0, 1.8f, 1, 0) == 0));
Test("every Unity source parses as C# 9", () => {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
    Check(dir != null, "Cannot locate project assets");
    foreach (var file in Directory.GetFiles(Path.Combine(dir.FullName, "Assets"), "*.cs", SearchOption.AllDirectories))
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: new[] { "UNITY_EDITOR", "UNITY_ANDROID", "UNITY_6000_0_OR_NEWER" }), file);
        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Check(errors.Length == 0, string.Join("\n", errors.Select(e => e.ToString())));
    }
});

var campaign = JsonSerializer.Deserialize<CampaignData>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "campaign.json")), options);
Test("campaign has fifty sequential validated levels", () => {
    Check(campaign.version == 1 && campaign.levels.Length == 50);
    for (int i = 0; i < 50; i++) Check(campaign.levels[i].id == i + 1 && LevelValidation.Error(campaign.levels[i]) == null, "Invalid campaign level " + (i + 1));
});
foreach (var campaignLevel in campaign.levels)
{
    var data = campaignLevel;
    Test($"campaign {data.id:00} all cargo can complete within timer without collision", () => {
        var sim = new CampaignSimulation(cfg); sim.Load(data);
        foreach (var solution in data.solutions)
        {
            var path = new PathData(256);
            foreach (var point in solution.points) path.Add(point.Vector, data.maxPathLength, 0);
            Check(sim.Launch(solution.cargo, path), "Route refused");
            int ticks = 0;
            while (sim.Shuttle(solution.cargo).Phase != 4 && sim.Outcome == 0 && ticks++ < 1200)
            {
                sim.Step(1f / 60);
                Check(sim.Attempts == 0, $"Collision at {sim.Shuttle(solution.cargo).Position.X}, {sim.Shuttle(solution.cargo).Position.Y}");
            }
            Check(sim.Shuttle(solution.cargo).Phase == 4, $"Cargo {solution.cargo} failed; phase={sim.Shuttle(solution.cargo).Phase}, position={sim.Shuttle(solution.cargo).Position.X},{sim.Shuttle(solution.cargo).Position.Y}");
        }
        Check(sim.Outcome == 1 && sim.Delivered == data.cargo.Length && sim.Stars >= 1, "No win");
    });
}
Test("barrier swept collision catches crossings", () => Check(CollisionMath.SegmentBox(new Float2(-2, 0), new Float2(2, 0), default, .5f, .1f)));
Test("timed failure fires once and cannot become a win", () => {
    var sim = new CampaignSimulation(cfg); sim.Load(campaign.levels[10]); int calls = 0; sim.Finished += result => calls++;
    for (int i = 0; i < 5000; i++) sim.Step(1f / 60);
    Check(sim.Outcome == 2 && calls == 1 && sim.Stars == 0);
});
Test("unstepped paused simulation has no timer drift", () => { var sim = new CampaignSimulation(cfg); sim.Load(campaign.levels[10]); float remaining = sim.Remaining; Check(sim.Remaining == remaining && sim.Elapsed == 0); });
Test("gravity pulls inward and barrier repels outward", () => {
    var well = new HazardDefinition { kind = 2, radius = 2, strength = 2 };
    Check(well.ForceAt(new Float2(1, 0), 0).X < 0);
    well.kind = 1; Check(well.ForceAt(new Float2(1, 0), 0).X > 0);
});


Test("oversized local save is bounded and resets safely", () => WithStore((dir,store) => {
    File.WriteAllText(Path.Combine(dir,"zerog_save.json"), new string('x',70000));
    Check(store.Load().highestUnlockedLevel==1 && store.WasReset);
}));
Test("malformed level data and dependency cycles are rejected", () => {
    var level = JsonSerializer.Deserialize<LevelDefinition>(JsonSerializer.Serialize(campaign.levels[34],options),options);
    level.bays[0].unlockAfter=0; Check(LevelValidation.Error(level)!=null);
    level.bays[0].unlockAfter=-1; level.hazards[0].radius=float.NaN; Check(LevelValidation.Error(level)!=null);
});
Test("later sectors tighten measurable challenge constraints", () => {
    for(int i=1;i<50;i++) {
        Check(campaign.levels[i].drift>campaign.levels[i-1].drift);
        Check(campaign.levels[i].hazards.Length>=campaign.levels[i-1].hazards.Length);
        if(i>10) Check(campaign.levels[i].timeLimit/campaign.levels[i].cargo.Length < campaign.levels[i-1].timeLimit/campaign.levels[i-1].cargo.Length);
    }
});
Test("completion and dock events are emitted once", () => {
    var level=campaign.levels[0]; var sim=new CampaignSimulation(cfg); sim.Load(level); int wins=0,docks=0;
    sim.Finished+=value=>wins++; sim.Docked+=value=>docks++;
    var path=new PathData(256); foreach(var p in level.solutions[0].points) path.Add(p.Vector,6,0);
    Check(sim.Launch(0,path)); Check(!sim.Launch(0,path));
    for(int i=0;i<2000;i++) sim.Step(1f/60);
    Check(wins==1 && docks==1 && sim.Outcome==1);
});
Test("chain bay stays locked until its dependency is delivered", () => {
    var level=campaign.levels[34]; var sim=new CampaignSimulation(cfg); sim.Load(level);
    Check(sim.BayUnlocked(0) && !sim.BayUnlocked(1) && !sim.BayUnlocked(2));
    var path=new PathData(256); foreach(var p in level.solutions[0].points) path.Add(p.Vector,6,0);
    sim.Launch(0,path); for(int i=0;i<1200 && sim.Shuttle(0).Phase!=4;i++) sim.Step(1f/60);
    Check(sim.BayUnlocked(1) && !sim.BayUnlocked(2));
});

int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception e) { failed++; Console.Error.WriteLine("FAIL " + test.Name + ": " + e.Message); }
}
Console.WriteLine($"{tests.Count - failed} passed, {failed} failed, {tests.Count} executed");
return failed == 0 ? 0 : 1;
