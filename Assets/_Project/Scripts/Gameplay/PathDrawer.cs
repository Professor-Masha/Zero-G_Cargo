using System;
using UnityEngine;
using UnityEngine.EventSystems;
using ZeroGCargo.Core;
using ZeroGCargo.Data;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Single-pointer path input with pickup rejection and preallocated route storage.</summary>
    public sealed class PathDrawer
    {
        private readonly GameConfig game;
        private readonly InertiaConfig inertia;
        private readonly Camera camera;
        private bool drawing;
        private int pointerId;
        /// <summary>Reusable accepted or in-progress route.</summary>
        public PathData Path { get; }
        /// <summary>Raised once when a sufficiently long route is released.</summary>
        public event Action<PathData> OnPathComplete;
        /// <summary>Creates input state and its fixed-capacity route buffer.</summary>
        public PathDrawer(Camera camera, GameConfig game, InertiaConfig inertia)
        { this.camera = camera; this.game = game; this.inertia = inertia; Path = new PathData(game.PathCapacity); }
        /// <summary>Cancels an active pointer without launching the route.</summary>
        public void Cancel() { drawing = false; }
        /// <summary>Reads mouse or the original touch only; ignores UI and additional touches.</summary>
        public void Tick(Vector2 shuttle, bool canDraw)
        {
            if (!canDraw) { Cancel(); return; }
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !drawing) Begin(touch.position, touch.fingerId, shuttle);
                    if (!drawing || touch.fingerId != pointerId) continue;
                    if (touch.phase == TouchPhase.Canceled) { Cancel(); return; }
                    Sample(touch.position);
                    if (touch.phase == TouchPhase.Ended) End();
                }
                return;
            }
            if (drawing && pointerId != -1) { Cancel(); return; }
            if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition, -1, shuttle);
            if (drawing && Input.GetMouseButton(0)) Sample(Input.mousePosition);
            if (drawing && Input.GetMouseButtonUp(0)) { Sample(Input.mousePosition); End(); }
        }
        private Vector2 World(Vector2 screen) => camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
        private void Begin(Vector2 screen, int id, Vector2 shuttle)
        {
            if (EventSystem.current != null && (id == -1 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(id))) return;
            if (Vector2.Distance(World(screen), shuttle) > game.PickupRadius) return;
            Path.Clear(); Path.Add(new Float2(shuttle.x, shuttle.y), inertia.MaxPathLength, 0);
            drawing = true; pointerId = id;
        }
        private void Sample(Vector2 screen)
        {
            Vector2 p = World(screen);
            p.x = Mathf.Clamp(p.x, -game.BoundaryX, game.BoundaryX); p.y = Mathf.Clamp(p.y, -game.BoundaryY, game.BoundaryY);
            Path.Add(new Float2(p.x, p.y), inertia.MaxPathLength, game.PointSpacing);
        }
        private void End()
        {
            drawing = false;
            if (Path.Length >= game.MinimumPathLength) OnPathComplete?.Invoke(Path);
            else Path.Clear();
        }
    }
}
