using System;
using UnityEngine;
using UnityEngine.EventSystems;
using ZeroGCargo.Core;
using ZeroGCargo.Data;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Single-pointer, UI-aware drawing on the 3D station's XZ plane.</summary>
    public sealed class PathDrawer
    {
        private readonly Camera camera;
        private readonly GameConfig config;
        private readonly Plane plane = new Plane(Vector3.up, Vector3.zero);
        private bool drawing;
        private int pointer;
        private float budget;
        /// <summary>Reusable input buffer.</summary>
        public PathData Path { get; }
        /// <summary>Selected shuttle, or -1 before a selection.</summary>
        public int Selected { get; private set; } = -1;
        /// <summary>Emitted once per released route.</summary>
        public event Action<int,PathData> OnPathComplete;
        /// <summary>Creates a fixed input buffer.</summary>
        public PathDrawer(Camera camera, GameConfig config) { this.camera = camera; this.config = config; Path = new PathData(config.PathCapacity); }
        /// <summary>Cancels the pointer without launching a route.</summary>
        public void Cancel() { drawing = false; Selected = -1; Path.Clear(); }
        private Float2 World(Vector2 screen)
        {
            Ray ray = camera.ScreenPointToRay(screen);
            if (!plane.Raycast(ray,out float distance)) return new Float2(float.NaN,float.NaN);
            Vector3 p = ray.GetPoint(distance); return new Float2(p.x,p.z);
        }
        /// <summary>Reads the original pointer only, rejecting additional touches and UI.</summary>
        public void Tick(CampaignSimulation simulation)
        {
            if (simulation.Outcome != 0) { Cancel(); return; }
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !drawing) Begin(touch.position,touch.fingerId,simulation);
                    if (!drawing || touch.fingerId != pointer) continue;
                    if (touch.phase == TouchPhase.Canceled) { Cancel(); return; }
                    Sample(touch.position); if (touch.phase == TouchPhase.Ended) End();
                }
                return;
            }
            if (drawing && pointer != -1) { Cancel(); return; }
            if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition,-1,simulation);
            if (drawing && Input.GetMouseButton(0)) Sample(Input.mousePosition);
            if (drawing && Input.GetMouseButtonUp(0)) { Sample(Input.mousePosition); End(); }
        }
        private void Begin(Vector2 screen, int id, CampaignSimulation simulation)
        {
            if (EventSystem.current != null && (id == -1 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(id))) return;
            Float2 point = World(screen); float nearest = config.PickupRadius; int selected = -1;
            for (int i = 0; i < simulation.Level.cargo.Length; i++)
            { var shuttle = simulation.Shuttle(i); float distance = (point-shuttle.Position).Magnitude; if (shuttle.CanDraw && distance < nearest) { nearest = distance; selected = i; } }
            if (selected < 0) return;
            Selected = selected; pointer = id; drawing = true; budget = simulation.Level.maxPathLength;
            Path.Clear(); Path.Add(simulation.Shuttle(selected).Position,budget,0);
        }
        private void Sample(Vector2 screen)
        {
            Float2 p = World(screen); if (!p.IsFinite) return;
            p = new Float2(Mathf.Clamp(p.X,-config.BoundaryX,config.BoundaryX),Mathf.Clamp(p.Y,-config.BoundaryY,config.BoundaryY));
            Path.Add(p,budget,config.PointSpacing);
        }
        private void End()
        {
            drawing = false;
            if (Path.Length >= config.MinimumPathLength) OnPathComplete?.Invoke(Selected,Path);
            Path.Clear(); Selected = -1;
        }
    }
}
