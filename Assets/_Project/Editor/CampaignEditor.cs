using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ZeroGCargo.Core;

namespace ZeroGCargo.Editor
{
    /// <summary>Local level-authoring tool with validation and simulated solution checks.</summary>
    public sealed class CampaignEditor : EditorWindow
    {
        private const string PathName = "Assets/_Project/Resources/Levels/campaign.json";
        private CampaignData data;
        private int selected;
        private Vector2 scroll;
        private string report = "Select a level, edit its layout and validate before saving.";
        /// <summary>Opens the campaign authoring tool.</summary>
        [MenuItem("Zero-G Cargo/Campaign editor")]
        public static void Open() => GetWindow<CampaignEditor>("Cargo campaign");
        private void OnEnable() { Reload(); }
        private void Reload() { if (File.Exists(PathName)) data = JsonUtility.FromJson<CampaignData>(File.ReadAllText(PathName)); }
        private static PointData Point(string label,PointData point)
        {
            Vector2 value = EditorGUILayout.Vector2Field(label,new Vector2(point.x,point.y));
            return new PointData { x = Mathf.Round(value.x*20)/20, y = Mathf.Round(value.y*20)/20 };
        }
        private void OnGUI()
        {
            if (data == null || data.levels == null) { if(GUILayout.Button("Reload campaign")) Reload(); return; }
            selected = EditorGUILayout.IntSlider("Level",selected,0,data.levels.Length-1);
            var level = data.levels[selected]; scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Sector " + level.id,EditorStyles.boldLabel);
            level.title = EditorGUILayout.TextField("Title",level.title);
            level.timeLimit = EditorGUILayout.Slider("Time limit (0=untimed)",level.timeLimit,0,180);
            level.drift = EditorGUILayout.Slider("Drift",level.drift,.05f,.35f);
            level.maxPathLength = EditorGUILayout.Slider("Path budget",level.maxPathLength,3,10);
            for(int i=0;i<level.cargo.Length;i++)
            {
                EditorGUILayout.Space(); EditorGUILayout.LabelField("Cargo " + (i+1),EditorStyles.boldLabel);
                level.cargo[i].spawn = Point("Spawn",level.cargo[i].spawn); level.bays[i].position = Point("Bay",level.bays[i].position);
                level.bays[i].unlockAfter = EditorGUILayout.IntSlider("Unlock after cargo (-1=none)",level.bays[i].unlockAfter,-1,i-1);
                for(int p=0;p<level.solutions[i].points.Length;p++) level.solutions[i].points[p] = Point("Guide " + p,level.solutions[i].points[p]);
            }
            for(int i=0;i<level.hazards.Length;i++)
            {
                var h=level.hazards[i]; EditorGUILayout.Space(); EditorGUILayout.LabelField("Hazard " + i + " / " + h.kind,EditorStyles.boldLabel);
                h.position = Point("Position",h.position); h.radius = EditorGUILayout.Slider("Radius",h.radius,.05f,3); h.strength = EditorGUILayout.Slider("Force",h.strength,0,20);
            }
            if(GUILayout.Button("Validate and simulate this level")) report = ValidateAndSolve(level);
            if(GUILayout.Button("Save validated campaign"))
            {
                string error = null; foreach(var item in data.levels) { error = LevelValidation.Error(item); if(error != null) break; }
                if(error != null) report = "Not saved: " + error;
                else { File.WriteAllText(PathName,JsonUtility.ToJson(data,true)); AssetDatabase.Refresh(); report = "Saved. Re-run full campaign tests after layout changes."; }
            }
            if(GUILayout.Button("Discard unsaved edits and reload")) Reload();
            EditorGUILayout.HelpBox(report,MessageType.Info); EditorGUILayout.EndScrollView();
        }
        /// <summary>Checks the authored guide against the same simulation used in the game.</summary>
        public static string ValidateAndSolve(LevelDefinition level)
        {
            string error = LevelValidation.Error(level); if(error != null) return error;
            var sim = new CampaignSimulation(new FlightSettings(8,6,.4f,12,.15f,.3f)); sim.Load(level);
            foreach(var solution in level.solutions)
            {
                var path = new PathData(256); foreach(var p in solution.points) path.Add(p.Vector,level.maxPathLength,0);
                if(!sim.Launch(solution.cargo,path)) return "Guide could not launch";
                int ticks=0; while(sim.Shuttle(solution.cargo).Phase!=4 && sim.Outcome==0 && ticks++<1200) sim.Step(1f/60);
                if(sim.Attempts>0 || sim.Shuttle(solution.cargo).Phase!=4) return "Guide collided, stopped outside bay or timed out";
            }
            return sim.Outcome==1 ? "Valid and solvable in simulation. Human difficulty testing is still required." : "Guide did not complete";
        }
    }
}
